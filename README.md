# March Madness Bracket Simulator

A Blazor WebAssembly app for filling out and simulating NCAA March Madness brackets. Supports historical tournaments (2015–2026, excluding 2020) with real team stats and actual results for accuracy comparison.

**Live site:** https://rongner.github.io/MarchMadnessBlazor/

---

## Features

- **Manual bracket picking** — click any team to advance them round by round
- **Simulation modes** (Settings page):
  - *Random* — 50/50 coin flip
  - *Seed-Weighted* — favourites win more often; adjustable upset frequency via slider
  - *Chalk* — always advances the better seed, no upsets
  - *Stats* — deterministic possession-chain model using real team stats (pace, shooting splits, turnover rates, rebounding, free throws)
- **Historical years** — switch between 2015–2026 via the toolbar dropdown
- **Accuracy overlay** — after simulating, games the model got wrong show the actual winner and final score; toolbar shows overall accuracy percentage
- **Score tracking** — standard bracket scoring (10/20/40/80/160/320 points per round)

---

## Running Locally

```bash
dotnet run
```

Requires .NET 10 SDK. The app loads tournament data from `wwwroot/data/tournament-{year}.json`. If a year's file is missing it falls back to hardcoded 2024 teams.

### Fetching tournament data

```bash
pip install requests beautifulsoup4
python scripts/fetch_tournament_data.py --years 2024 2025 2026
```

Data sources:
- **Barttorvik** `{year}_fffinal.csv` — 13 team stats (shooting, turnovers, rebounding, free throws)
- **WarrenNolan** `stats-adv-pace` — pace / tempo
- **NCAA API** (`ncaa-api.henrygd.me`) — bracket seedings and actual results

Stats fetching is non-fatal — if a source is unavailable, teams get `stats: null` and Stats mode falls back to Chalk for those matchups.

---

## CI / GitHub Pages

The `fetch-data` job runs on push and can be triggered manually (Actions → CI → Run workflow) with an optional `years` input. It commits updated JSON files to `wwwroot/data/` and the `build` job then publishes to GitHub Pages.

To add data for new years, trigger the workflow:

```
years: 2025 2026
```

---

## Future Development

- **Toolbar score display is unclear** — the top-right corner shows `#/#` before and after simulation (picks progress, then accuracy vs. actual); the format is confusing without context; consider clearer labels or a tooltip
- **Show projected scores in Stats mode** — `ExpectedScore()` already computes per-team expected point totals but discards them; surface these in the game slot after simulation
- **Team name alias coverage** — some teams may get `stats: null` after a fetch due to name mismatches between Barttorvik and the NCAA API; expand `NAME_ALIASES` in `fetch_tournament_data.py` as mismatches are found
- **"No data" message for missing years** — currently falls back silently to 2024 hardcoded teams when a year's JSON is absent; show an explicit notice instead
- **2020 gap in year selector** — the dropdown jumps 2019 → 2021; could add a disabled/labelled placeholder to explain the gap
