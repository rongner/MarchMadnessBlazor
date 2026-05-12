# MarchMadnessBlazor — Project Status

## Goal

Portfolio project demonstrating Blazor WASM and C# skills. Simulates a full
64-team NCAA March Madness bracket — users fill out their picks, then run a
simulation to score them against a randomly generated (seeding-weighted) result.

---

## Tech Stack

- **Blazor WASM** — runs entirely in the browser, no backend required
- **C# simulation engine** — seeding-weighted random outcomes, ELO optional
- **Hosting** — GitHub Pages or Netlify (static, free)

---

## Requirements

### Bracket Engine
- [ ] 64-team bracket data model (6 rounds, 63 games)
- [ ] Seeding-weighted simulation (higher seeds win more often)
- [ ] User bracket picker — click to advance a team each round
- [ ] Simulate remaining games and score user bracket vs result
- [ ] Scoring: standard ESPN rules (10 / 20 / 40 / 80 / 160 / 320 pts per round)

### UI
- [ ] Visual bracket — left side / right side, Final Four in center
- [ ] Team name + seed displayed per slot
- [ ] Highlight correct picks (green) and wrong picks (red) after simulation
- [ ] Reset button — clear bracket and start over
- [ ] "Simulate All" button — auto-fill remaining games
- [ ] Score summary panel — points earned per round, total

### Data
- [ ] Static team list for a sample 64-team field (can be fictional or real)
- [ ] Seedings 1–16 per region (4 regions: East, West, South, Midwest)

### Project Setup
- [ ] Blazor WASM project scaffolded
- [ ] GitHub repo with CI (build + publish)
- [ ] Deployed to GitHub Pages

---

## Optional Enhancements

- [ ] ELO-based simulation instead of pure seed weighting
- [ ] Multiple bracket entries (compare several brackets side by side)
- [ ] Animated simulation (games resolve one by one with delay)
- [ ] Real team data loaded from JSON file
- [ ] Mobile-responsive layout

---

## Status

Project not yet started. Next step: scaffold Blazor WASM project.
