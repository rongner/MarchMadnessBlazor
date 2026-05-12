# MarchMadnessBlazor — Project Status

## Goal

Portfolio project demonstrating Blazor WASM and C# skills. Simulates a full
64-team NCAA March Madness bracket — users fill out picks, run a simulation,
and score against a generated or historical result.

Live at: https://rongner.github.io/MarchMadnessBlazor/

---

## Tech Stack

- **Blazor WASM** — runs entirely in the browser, no backend required
- **C# simulation engine** — multiple simulation modes
- **Hosting** — GitHub Pages (static, free)
- **CI** — GitHub Actions: build → publish → deploy to Pages

---

## Completed

- [x] 64-team bracket data model (6 rounds, 63 games)
- [x] 2024 NCAA tournament teams with real seedings
- [x] Seeding-weighted simulation (`P(top wins) = bottom.Seed / (top.Seed + bottom.Seed)`)
- [x] User bracket picker — click to advance a team each round
- [x] Cascade clearing — changing a pick nulls invalid downstream picks
- [x] Simulate button — auto-fills remaining picks then runs simulation
- [x] Scoring: ESPN rules (10 / 20 / 40 / 80 / 160 / 320 pts per round, max 1920)
- [x] Visual bracket — left half / right half, Final Four + Championship in center
- [x] Correct picks (green), wrong picks (red), sim winner (underlined gold)
- [x] Reset button
- [x] Score summary panel (total + per-round breakdown)
- [x] Simulation Settings page (`/settings`):
  - Random mode (50/50)
  - Seed-Weighted mode (default, adjustable influence slider 0.3×–3.0×)
  - Chalk mode (always pick lower seed)
- [x] GitHub Pages deploy via CI

---

## In Progress — Stats-Based Simulation Mode

### Model Design (complete, not yet coded)

A possession-chain simulation that computes expected scores deterministically
from real team stats. No randomness — whoever scores higher wins.

#### Step 1 — Base possessions
```
base_poss = avg(Pace_A, Pace_B)
```

#### Step 2 — Turnovers (reduce possessions)
```
effective_to_rate_A = avg(A.TO%, B."TO% Def")
shooting_poss_A     = base_poss × (1 − effective_to_rate_A)
```
`TO% Def` = opponent forced-turnover rate (covers steals + all forced TOs).

#### Step 3 — Shot mix and effective shooting %
```
effective_3_rate_A  = avg(A."3P rate",  B."3P rate D")
effective_3P%_A     = avg(A.3P%,        B."3pD%")
effective_2P%_A     = avg(A.2p%,        B."2p%D")

miss_rate_A = effective_3_rate_A  × (1 − effective_3P%_A)
            + (1 − effective_3_rate_A) × (1 − effective_2P%_A)
```

#### Step 4 — Offensive rebounds (add possessions)
```
orb_rate_A     = avg(A."OR%", 1 − B."DR%")
total_poss_A   = shooting_poss_A × (1 + orb_rate_A × miss_rate_A)
```

#### Step 5 — Scoring (field goals)
```
pts_per_poss_A = effective_3_rate_A × effective_3P%_A × 3
              + (1 − effective_3_rate_A) × effective_2P%_A × 2
```

#### Step 6 — Free throws
```
effective_ftr_A  = avg(A.FTR, B."FTR Def")   // opponent affects how often you get fouled
ft_pts_per_pos_A = effective_ftr_A × A.FT%   // opponent CANNOT affect your FT%
```
Note: FTR in Barttorvik is FTA/FGA. Yields points per shot attempt, scaled by possession.

#### Final score
```
score_A = total_poss_A × (pts_per_poss_A + ft_pts_per_pos_A)
score_B = (same formula mirrored)
winner  = max(score_A, score_B)
```

#### Team stats required (14 per team)
| Barttorvik column | Meaning |
|---|---|
| `tempo` (from trank) | Pace — possessions per game |
| `TO%` | Turnover rate |
| `TO% Def.` | Forced turnover rate (covers steals) |
| `OR%` | Offensive rebound % |
| `DR%` | Defensive rebound % |
| `3P rate` | 3-point attempt rate per possession |
| `3P rate D` | Opponent 3-point attempt rate allowed |
| `3P%` | 3-point make % |
| `3pD%` | Opponent 3-point make % allowed |
| `2p%` | 2-point make % |
| `2p%D` | Opponent 2-point make % allowed |
| `FTR` | Free throw rate (FTA/FGA) |
| `FTR Def` | Opponent free throw rate allowed |
| `ft%` | Free throw % (no opponent adjustment) |

---

## Planned — Historical Tournaments

### Goal
Support any tournament year (~2015–2024). Show real bracket, simulate with
real stats, compare predicted vs actual results.

### Architecture — Option A (chosen)
- GitHub Actions CI script (Python) fetches data each April
- Outputs `wwwroot/data/tournament-{year}.json` per year
- Blazor loads static JSON files at runtime (same origin, no CORS)
- No backend required — stays on GitHub Pages

### Data Sources
- **Barttorvik** `YEAR_fffinal.csv` — all 13 shooting/rebounding/turnover stats
- **Barttorvik** `trank.php?year=YEAR&csv=1` — tempo/pace (Cloudflare-protected;
  may need headless fetch or alternative source)
- **Tempo fallback** — WarrenNolan or TeamRankings possessions-per-game if
  Barttorvik tempo endpoint stays blocked
- **Bracket + actual results** — NCAA data API or similar (TBD — needs research)
  for: seedings, who played who each round, actual scores

### Status of data source research
- `YEAR_fffinal.csv` confirmed accessible and has 13 of 14 needed stats
- `tempo` column NOT in fffinal.csv — needs separate trank endpoint (blocked by
  Cloudflare during research; workaround needed)
- Bracket/actual results source not yet confirmed — `henrygd/ncaa-api` (GitHub)
  is a candidate; needs verification

### JSON schema (planned)
```json
{
  "year": 2024,
  "teams": [
    {
      "id": 0, "name": "UConn", "seed": 1, "region": "East",
      "stats": {
        "pace": 67.2, "to_pct": 14.2, "to_def_pct": 18.1,
        "or_pct": 33.5, "dr_pct": 76.5,
        "three_rate": 0.38, "three_rate_d": 0.31,
        "three_pct": 0.385, "three_pct_d": 0.295,
        "two_pct": 0.562, "two_pct_d": 0.448,
        "ftr": 0.285, "ftr_def": 0.21, "ft_pct": 0.732
      }
    }
  ],
  "results": {
    "1": [0, 2, 4, ...],
    "2": [...],
    "6": [0]
  },
  "scores": {
    "1": [{"w": 85, "l": 62}, ...]
  }
}
```

### UI changes needed
- Year dropdown in toolbar
- Stats mode in simulation settings
- After simulation: wrong games show actual winner + score in red
- Accuracy summary: "47 / 63 correct — 74.6%"

---

## Next Steps (priority order)

1. Resolve tempo data source (trank Cloudflare workaround or alternative)
2. Confirm bracket + actual results API endpoint
3. Write Python CI fetch script
4. Build C# `TeamStats` model + `TournamentDataService`
5. Implement possession-chain simulation in `BracketService`
6. Add Stats mode to Settings page
7. Year selector UI + accuracy overlay
