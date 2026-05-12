#!/usr/bin/env python3
"""
Fetches NCAA tournament bracket data, team stats, and actual results for each year
and writes one JSON file per year to wwwroot/data/tournament-{year}.json.

Data sources:
  - Barttorvik  YEAR_fffinal.csv     → 13 of 14 team stats
  - WarrenNolan stats-adv-pace page  → pace / tempo
  - henrygd/ncaa-api                 → bracket structure, seeds, actual results + scores

Usage:
    pip install requests beautifulsoup4
    python scripts/fetch_tournament_data.py [--years 2022 2023 2024]
"""

import argparse
import csv
import io
import json
import re
import time
from pathlib import Path

import requests
from bs4 import BeautifulSoup

# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------

YEARS = list(range(2015, 2025))

OUT_DIR = Path(__file__).parent.parent / "wwwroot" / "data"

# Regions in bracket slot order (matches our C# TournamentData layout).
# East=slots 0-15, South=16-31, West=32-47, Midwest=48-63.
REGIONS_ORDER = ["East", "South", "West", "Midwest"]

# Seed pair order within each region: 1v16, 8v9, 5v12, 4v13, 6v11, 3v14, 7v10, 2v15.
SEED_PAIR_ORDER = [(1, 16), (8, 9), (5, 12), (4, 13), (6, 11), (3, 14), (7, 10), (2, 15)]

# Some teams are known by different names across data sources.
# Maps Barttorvik/WarrenNolan names → NCAA API short names.
NAME_ALIASES: dict[str, str] = {
    "Connecticut":          "UConn",
    "N.C. State":           "NC State",
    "North Carolina State": "NC State",
    "Miami FL":             "Miami",
    "Miami (FL)":           "Miami",
    "Saint Mary's":         "St. Mary's",
    "Texas Christian":      "TCU",
    "Loyola Chicago":       "Loyola Chicago",
    "USC":                  "Southern California",
    "UNLV":                 "Nevada-Las Vegas",
    "VCU":                  "Virginia Commonwealth",
    "UAB":                  "Alabama-Birmingham",
    "FAU":                  "Florida Atlantic",
    "BYU":                  "Brigham Young",
    "UCSB":                 "UC Santa Barbara",
    "UNC Asheville":        "North Carolina-Asheville",
    "Col. of Charleston":   "Charleston",
    "Long Island Univ.":    "Long Island University",
    "South Dakota St.":     "South Dakota State",
    "Long Beach State":     "Long Beach State",
    "Montana State":        "Montana State",
    "Colorado State":       "Colorado State",
    "McNeese":              "McNeese State",
}

HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (compatible; MarchMadnessBracket/1.0; "
        "github.com/rongner/MarchMadnessBlazor)"
    )
}


# ---------------------------------------------------------------------------
# Source 1: Barttorvik fffinal.csv — 13 stats
# ---------------------------------------------------------------------------

FFFINAL_COLS = {
    # (csv_header,          our_key)
    "3P%":       "three_pct",
    "3pD%":      "three_pct_d",
    "2p%":       "two_pct",
    "2p%D":      "two_pct_d",
    "3P rate":   "three_rate",
    "3P rate D": "three_rate_d",
    "ft%":       "ft_pct",
    "FTR":       "ftr",
    "FTR Def":   "ftr_def",
    "OR%":       "or_pct",
    "DR%":       "dr_pct",
    "TO%":       "to_pct",
    "TO% Def.":  "to_def_pct",
}


def fetch_barttorvik_stats(year: int) -> dict[str, dict]:
    url = f"https://barttorvik.com/{year}_fffinal.csv"
    resp = requests.get(url, headers=HEADERS, timeout=30)
    resp.raise_for_status()

    reader = csv.DictReader(io.StringIO(resp.text))
    result = {}
    for row in reader:
        name = row.get("TeamName", "").strip()
        if not name:
            continue
        stats = {}
        for col, key in FFFINAL_COLS.items():
            try:
                stats[key] = float(row[col])
            except (KeyError, ValueError):
                stats[key] = 0.0
        result[name] = stats
    return result


# ---------------------------------------------------------------------------
# Source 2: WarrenNolan — pace
# ---------------------------------------------------------------------------

def fetch_pace(year: int) -> dict[str, float]:
    url = f"https://www.warrennolan.com/basketball/{year}/stats-adv-pace"
    resp = requests.get(url, headers=HEADERS, timeout=30)
    resp.raise_for_status()

    soup = BeautifulSoup(resp.text, "html.parser")
    result = {}
    for row in soup.select("table tr"):
        cells = row.find_all(["td", "th"])
        if len(cells) < 3:
            continue
        name_cell = cells[1]
        pace_cell = cells[2]
        name = name_cell.get_text(strip=True)
        try:
            pace = float(pace_cell.get_text(strip=True))
        except ValueError:
            continue
        if name and pace:
            result[name] = pace
    return result


# ---------------------------------------------------------------------------
# Source 3: henrygd/ncaa-api — bracket, seeds, results, scores
# ---------------------------------------------------------------------------

NCAA_API_BASE = "https://ncaa-api.henrygd.me"


def fetch_ncaa_bracket(year: int) -> dict:
    url = f"{NCAA_API_BASE}/brackets/basketball-men/d1/{year}"
    resp = requests.get(url, headers=HEADERS, timeout=30)
    resp.raise_for_status()
    return resp.json()


def parse_bracket(raw: dict) -> tuple[list[dict], dict, dict]:
    """
    Returns:
      teams_by_region: {region: {seed: name}}
      results[round][pos] = winning team name  (round 1-indexed, pos 0-indexed)
      scores[round][pos]  = (winner_score, loser_score)
    """
    teams_by_region: dict[str, dict[int, str]] = {r: {} for r in REGIONS_ORDER}
    results: dict[int, dict[int, str]] = {r: {} for r in range(1, 7)}
    scores:  dict[int, dict[int, tuple]] = {r: {} for r in range(1, 7)}

    # The NCAA API returns a nested structure.  We walk every game object we
    # can find, keying on bracketPositionId to infer round + position.
    games = _collect_games(raw)

    for game in games:
        teams = game.get("teams", [])
        if len(teams) != 2:
            continue

        # Determine round from number of games that could have preceded this.
        # bracketPositionId is 1-based and encodes position across all rounds.
        # Round 1: positions 1-32, Round 2: 33-48, etc. (NCAA layout varies by year).
        # We derive round from round_number or title metadata if available.
        rnd = _infer_round(game)
        if rnd is None or rnd < 1 or rnd > 6:
            continue

        pos = _infer_position(game, rnd)
        if pos is None:
            continue

        # Populate teams_by_region from round-1 games (seeds are reliable there).
        if rnd == 1:
            for t in teams:
                region = _infer_region(game)
                if region:
                    seed = t.get("seed", 0)
                    name = t.get("nameShort", t.get("nameFull", ""))
                    if seed and name:
                        teams_by_region[region][seed] = name

        # Record result if the game has a winner.
        winner = next((t for t in teams if t.get("isWinner")), None)
        loser  = next((t for t in teams if not t.get("isWinner")), None)
        if winner:
            results[rnd][pos] = winner.get("nameShort", winner.get("nameFull", ""))
        if winner and loser:
            ws = winner.get("score")
            ls = loser.get("score")
            if ws is not None and ls is not None:
                try:
                    scores[rnd][pos] = (int(ws), int(ls))
                except (TypeError, ValueError):
                    pass

    return teams_by_region, results, scores


def _collect_games(node, depth=0) -> list[dict]:
    """Recursively collect all game objects from the NCAA API response."""
    games = []
    if isinstance(node, dict):
        if "teams" in node and isinstance(node["teams"], list):
            games.append(node)
        for v in node.values():
            games.extend(_collect_games(v, depth + 1))
    elif isinstance(node, list):
        for item in node:
            games.extend(_collect_games(item, depth + 1))
    return games


def _infer_round(game: dict) -> int | None:
    # Try explicit round field first.
    for key in ("roundNumber", "round", "roundNum"):
        if key in game:
            try:
                return int(game[key])
            except (TypeError, ValueError):
                pass
    # Fall back to bracketPositionId ranges (NCAA D1 men's basketball layout).
    # Round 1: 1–32, Round 2: 33–48, Round 3: 49–56, Round 4: 57–60,
    # Round 5: 61–62, Round 6: 63.
    bpid = game.get("bracketPositionId")
    if bpid is None:
        return None
    try:
        bpid = int(bpid)
    except (TypeError, ValueError):
        return None
    if   bpid <= 32: return 1
    elif bpid <= 48: return 2
    elif bpid <= 56: return 3
    elif bpid <= 60: return 4
    elif bpid <= 62: return 5
    else:            return 6


def _infer_position(game: dict, rnd: int) -> int | None:
    bpid = game.get("bracketPositionId")
    if bpid is None:
        return None
    try:
        bpid = int(bpid)
    except (TypeError, ValueError):
        return None
    # Convert bracketPositionId to 0-indexed position within the round.
    offsets = {1: 0, 2: 32, 3: 48, 4: 56, 5: 60, 6: 62}
    offset = offsets.get(rnd, 0)
    return bpid - offset - 1


def _infer_region(game: dict) -> str | None:
    raw = game.get("sectionId") or game.get("region") or game.get("regionName") or ""
    raw = str(raw).strip().title()
    for r in REGIONS_ORDER:
        if r in raw:
            return r
    return None


# ---------------------------------------------------------------------------
# Name normalisation
# ---------------------------------------------------------------------------

def normalise(name: str) -> str:
    """Lower-case, strip punctuation/whitespace for fuzzy matching."""
    name = NAME_ALIASES.get(name, name)
    return re.sub(r"[^a-z0-9]", "", name.lower())


def best_match(target: str, candidates: dict[str, any], threshold=0.0) -> str | None:
    norm_target = normalise(target)
    for cand in candidates:
        if normalise(cand) == norm_target:
            return cand
    # Partial prefix match as fallback.
    for cand in candidates:
        nc = normalise(cand)
        if norm_target.startswith(nc[:6]) or nc.startswith(norm_target[:6]):
            return cand
    return None


# ---------------------------------------------------------------------------
# Assemble tournament JSON
# ---------------------------------------------------------------------------

def build_tournament(year: int, bart_stats: dict, pace_data: dict,
                     teams_by_region: dict, results: dict, scores: dict) -> dict:
    teams = []  # 64 entries in bracket slot order

    for region in REGIONS_ORDER:
        region_teams = teams_by_region.get(region, {})
        for high_seed, low_seed in SEED_PAIR_ORDER:
            for seed in (high_seed, low_seed):
                name = region_teams.get(seed, f"Seed {seed} ({region})")

                # Look up stats
                bart_key = best_match(name, bart_stats)
                pace_key = best_match(name, pace_data)

                raw = bart_stats.get(bart_key, {}) if bart_key else {}
                pace = pace_data.get(pace_key, 0.0) if pace_key else 0.0

                stats = None
                if raw:
                    stats = {
                        "pace":        pace,
                        "toPct":       raw.get("to_pct", 0.0),
                        "toDefPct":    raw.get("to_def_pct", 0.0),
                        "orPct":       raw.get("or_pct", 0.0),
                        "drPct":       raw.get("dr_pct", 0.0),
                        "threeRate":   raw.get("three_rate", 0.0),
                        "threeRateD":  raw.get("three_rate_d", 0.0),
                        "threePct":    raw.get("three_pct", 0.0),
                        "threePctD":   raw.get("three_pct_d", 0.0),
                        "twoPct":      raw.get("two_pct", 0.0),
                        "twoPctD":     raw.get("two_pct_d", 0.0),
                        "ftr":         raw.get("ftr", 0.0),
                        "ftrDef":      raw.get("ftr_def", 0.0),
                        "ftPct":       raw.get("ft_pct", 0.0),
                    }

                teams.append({"name": name, "seed": seed, "region": region, "stats": stats})

    # Build results arrays: results[round_idx] (0-indexed round, so round 1 = index 0)
    results_arr  = []
    scores_arr   = []
    for rnd in range(1, 7):
        count = 64 >> rnd
        r_row = []
        s_row = []
        # Map winner names back to team ids (index in teams list)
        name_to_id = {normalise(t["name"]): i for i, t in enumerate(teams)}
        for pos in range(count):
            winner_name = results.get(rnd, {}).get(pos)
            if winner_name:
                norm = normalise(winner_name)
                # Try direct, then alias
                tid = name_to_id.get(norm)
                if tid is None:
                    alias = NAME_ALIASES.get(winner_name)
                    if alias:
                        tid = name_to_id.get(normalise(alias))
                r_row.append(tid)
            else:
                r_row.append(None)

            sc = scores.get(rnd, {}).get(pos)
            s_row.append(list(sc) if sc else None)

        results_arr.append(r_row)
        scores_arr.append(s_row)

    return {
        "year":    year,
        "teams":   teams,
        "results": results_arr,
        "scores":  scores_arr,
    }


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--years", nargs="+", type=int, default=YEARS)
    args = parser.parse_args()

    OUT_DIR.mkdir(parents=True, exist_ok=True)

    for year in args.years:
        print(f"\n── {year} ──────────────────────────────────")
        try:
            print(f"  Fetching Barttorvik stats …")
            bart = fetch_barttorvik_stats(year)
            print(f"    {len(bart)} teams")
            time.sleep(1)

            print(f"  Fetching WarrenNolan pace …")
            pace = fetch_pace(year)
            print(f"    {len(pace)} teams")
            time.sleep(1)

            print(f"  Fetching NCAA bracket …")
            raw_bracket = fetch_ncaa_bracket(year)
            teams_by_region, results, scores = parse_bracket(raw_bracket)
            print(f"    regions: {[r for r in REGIONS_ORDER if teams_by_region.get(r)]}")
            time.sleep(2)

            tournament = build_tournament(year, bart, pace, teams_by_region, results, scores)

            out_path = OUT_DIR / f"tournament-{year}.json"
            with open(out_path, "w", encoding="utf-8") as f:
                json.dump(tournament, f, indent=2)
            print(f"  Written {out_path}")

        except Exception as exc:
            print(f"  ERROR for {year}: {exc}")
            raise


if __name__ == "__main__":
    main()
