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

    private void NotifyChanged() => OnChange?.Invoke();
}
