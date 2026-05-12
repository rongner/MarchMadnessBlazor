using MarchMadnessBlazor.Models;

namespace MarchMadnessBlazor.Services;

public class BracketService
{
    private readonly Random _rng = new();

    public BracketState State { get; private set; } = new(TournamentData.GetInitialSeeding());

    public event Action? OnChange;

    public void Pick(int round, int position, Team team)
    {
        if (State.IsSimulated) return;
        State.SetUserPick(round, position, team);
        NotifyChanged();
    }

    // Fill all remaining empty user picks using seeding-weighted random.
    public void AutoFill()
    {
        if (State.IsSimulated) return;
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
        NotifyChanged();
    }

    // Simulate the actual tournament results (independent of user picks).
    public void Simulate()
    {
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

    // P(top wins) = bottom.Seed / (top.Seed + bottom.Seed) — higher seed favoured.
    private Team WeightedPick(Team top, Team bottom)
    {
        double pTop = (double)bottom.Seed / (top.Seed + bottom.Seed);
        return _rng.NextDouble() < pTop ? top : bottom;
    }

    private void NotifyChanged() => OnChange?.Invoke();
}
