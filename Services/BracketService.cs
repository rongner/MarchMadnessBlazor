using MarchMadnessBlazor.Models;

namespace MarchMadnessBlazor.Services;

public class BracketService(TournamentDataService dataService)
{
    private readonly Random _rng = new();

    public BracketState State { get; private set; } = new(TournamentData.GetInitialSeeding());
    public SimulationSettings Settings { get; set; } = new();
    public int SelectedYear { get; private set; } = 2024;
    public TournamentYear? CurrentYear { get; private set; }
    public bool HasActualResults => CurrentYear != null;

    public event Action? OnChange;

    public async Task LoadYearAsync(int year)
    {
        SelectedYear = year;
        var loaded = await dataService.LoadAsync(year);
        if (loaded != null)
        {
            CurrentYear = loaded;
            State = new BracketState(loaded.Teams);
        }
        else
        {
            CurrentYear = null;
            State = new BracketState(TournamentData.GetInitialSeeding());
        }
        NotifyChanged();
    }

    public Team? GetActualWinner(int round, int pos)
    {
        if (CurrentYear == null) return null;
        var id = CurrentYear.ActualResults[round][pos];
        if (id == null || id.Value >= CurrentYear.Teams.Length) return null;
        return CurrentYear.Teams[id.Value];
    }

    public GameScore? GetActualScore(int round, int pos) =>
        CurrentYear?.Scores[round][pos];

    public (int Correct, int Total) AccuracyVsActual()
    {
        if (!State.IsSimulated || CurrentYear == null) return (0, 0);
        int correct = 0, total = 0;
        for (int r = 1; r <= 6; r++)
        {
            int count = 64 >> r;
            for (int p = 0; p < count; p++)
            {
                var actualId = CurrentYear.ActualResults[r][p];
                if (actualId == null) continue;
                total++;
                if (State.SimResults[r][p]?.Id == actualId.Value) correct++;
            }
        }
        return (correct, total);
    }

    public void Pick(int round, int position, Team team)
    {
        if (State.IsSimulated) return;
        State.SetUserPick(round, position, team);
        NotifyChanged();
    }

    public void Simulate()
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
        State = new BracketState(CurrentYear?.Teams ?? TournamentData.GetInitialSeeding());
        NotifyChanged();
    }

    private Team WeightedPick(Team top, Team bottom) => Settings.Mode switch
    {
        SimMode.Random => _rng.NextDouble() < 0.5 ? top : bottom,
        SimMode.Chalk  => top.Seed <= bottom.Seed ? top : bottom,
        SimMode.Stats  => StatsPick(top, bottom),
        _              => SeededPick(top, bottom),
    };

    private Team SeededPick(Team top, Team bottom)
    {
        double k    = Settings.SeedInfluence;
        double wTop = Math.Pow(bottom.Seed, k);
        double wBot = Math.Pow(top.Seed, k);
        return _rng.NextDouble() < wTop / (wTop + wBot) ? top : bottom;
    }

    private static Team StatsPick(Team top, Team bottom)
    {
        if (top.Stats == null || bottom.Stats == null)
            return top.Seed <= bottom.Seed ? top : bottom;
        return ExpectedScore(top.Stats, bottom.Stats) >= ExpectedScore(bottom.Stats, top.Stats)
            ? top : bottom;
    }

    private static double ExpectedScore(TeamStats a, TeamStats b)
    {
        double basePoss     = (a.Pace + b.Pace) / 2.0;
        double toRate       = (a.ToPct / 100.0 + b.ToDefPct / 100.0) / 2.0;
        double shootingPoss = basePoss * (1.0 - toRate);
        double threeRate    = (a.ThreeRate  / 100.0 + b.ThreeRateD / 100.0) / 2.0;
        double threePct     = (a.ThreePct   / 100.0 + b.ThreePctD  / 100.0) / 2.0;
        double twoPct       = (a.TwoPct     / 100.0 + b.TwoPctD    / 100.0) / 2.0;
        double missRate     = threeRate * (1.0 - threePct) + (1.0 - threeRate) * (1.0 - twoPct);
        double orbRate      = (a.OrPct / 100.0 + (1.0 - b.DrPct / 100.0)) / 2.0;
        double totalPoss    = shootingPoss * (1.0 + orbRate * missRate);
        double ptsPer       = threeRate * threePct * 3.0 + (1.0 - threeRate) * twoPct * 2.0;
        double ftrA         = (a.Ftr / 100.0 + b.FtrDef / 100.0) / 2.0;
        double ftPts        = ftrA * (a.FtPct / 100.0);
        return totalPoss * (ptsPer + ftPts);
    }

    private void NotifyChanged() => OnChange?.Invoke();
}
