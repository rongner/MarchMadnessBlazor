using MarchMadnessBlazor.Models;

namespace MarchMadnessBlazor.Services;

public class BracketService
{
    private readonly Random _rng = new();

    public BracketState State { get; private set; } = new(TournamentData.GetInitialSeeding());
    public SimulationSettings Settings { get; set; } = new();

    public event Action? OnChange;

    public void Pick(int round, int position, Team team)
    {
        if (State.IsSimulated) return;
        State.SetUserPick(round, position, team);
        NotifyChanged();
    }

    // Fill any unpicked games then run the simulation.
    public void Simulate()
    {
        if (State.IsSimulated) return;
        // Auto-fill any missing user picks.
        for (int r = 1; r <= 6; r++)
        {
            int count = 64 >> r;
            for (int p = 0; p < count; p++)
            {
                if (State.UserPicks[r][p] != null) continue;
                var (top, bottom) = State.GetGameTeams(State.UserPicks, r, p);
                if (top != null && bottom != null)
                    State.UserPicks[r][p] = WeightedPick(top, bottom);
            }
        }
        // Simulate actual tournament results.
        for (int r = 1; r <= 6; r++)
        {
            int count = 64 >> r;
            for (int p = 0; p < count; p++)
            {
                var (top, bottom) = State.GetGameTeams(State.SimResults, r, p);
                if (top != null && bottom != null)
                    State.SimResults[r][p] = WeightedPick(top, bottom);
            }
        }
        State.IsSimulated = true;
        NotifyChanged();
    }

    public void Reset()
    {
        State = new BracketState(TournamentData.GetInitialSeeding());
        NotifyChanged();
    }

    private Team WeightedPick(Team top, Team bottom) => Settings.Mode switch
    {
        SimMode.Random => _rng.NextDouble() < 0.5 ? top : bottom,
        SimMode.Chalk  => top.Seed <= bottom.Seed ? top : bottom,
        SimMode.Stats  => StatsPick(top, bottom),
        _              => SeededPick(top, bottom),
    };

    // P(top wins) = bottom.Seed^k / (top.Seed^k + bottom.Seed^k); k=SeedInfluence.
    // k=1 → proportional to seed gap; k>1 → favourites win more; k<1 → more upsets.
    private Team SeededPick(Team top, Team bottom)
    {
        double k    = Settings.SeedInfluence;
        double wTop = Math.Pow(bottom.Seed, k);
        double wBot = Math.Pow(top.Seed, k);
        return _rng.NextDouble() < wTop / (wTop + wBot) ? top : bottom;
    }

    // Possession-chain model — fully deterministic, higher expected score wins.
    private static Team StatsPick(Team top, Team bottom)
    {
        if (top.Stats == null || bottom.Stats == null)
            return top.Seed <= bottom.Seed ? top : bottom; // fall back to chalk

        double scoreA = ExpectedScore(top.Stats,    bottom.Stats);
        double scoreB = ExpectedScore(bottom.Stats, top.Stats);
        return scoreA >= scoreB ? top : bottom;
    }

    private static double ExpectedScore(TeamStats a, TeamStats b)
    {
        // Step 1: base possessions = avg pace of the two teams.
        double basePoss = (a.Pace + b.Pace) / 2.0;

        // Step 2: turnovers (rates are 0-100; convert to 0-1).
        double toRate         = (a.ToPct / 100.0 + b.ToDefPct / 100.0) / 2.0;
        double shootingPoss   = basePoss * (1.0 - toRate);

        // Step 3: shot mix and effective shooting percentages.
        double threeRate      = (a.ThreeRate  / 100.0 + b.ThreeRateD / 100.0) / 2.0;
        double threePct       = (a.ThreePct   / 100.0 + b.ThreePctD  / 100.0) / 2.0;
        double twoPct         = (a.TwoPct     / 100.0 + b.TwoPctD    / 100.0) / 2.0;

        // Step 4: offensive rebounds extend possessions.
        double missRate       = threeRate * (1.0 - threePct) + (1.0 - threeRate) * (1.0 - twoPct);
        double orbRate        = (a.OrPct / 100.0 + (1.0 - b.DrPct / 100.0)) / 2.0;
        double totalPoss      = shootingPoss * (1.0 + orbRate * missRate);

        // Step 5: field goal points per possession.
        double ptsPer         = threeRate * threePct * 3.0 + (1.0 - threeRate) * twoPct * 2.0;

        // Step 6: free throw points per possession.
        // FTR is FTA/FGA × 100; convert to FTA per shooting possession then to points.
        double ftrA           = (a.Ftr    / 100.0 + b.FtrDef / 100.0) / 2.0;
        double ftPts          = ftrA * (a.FtPct / 100.0); // ~1 pt per FTA attempt drawn

        return totalPoss * (ptsPer + ftPts);
    }

    private void NotifyChanged() => OnChange?.Invoke();
}
