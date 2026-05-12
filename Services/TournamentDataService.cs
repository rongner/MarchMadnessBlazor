using System.Net.Http.Json;
using MarchMadnessBlazor.Models;

namespace MarchMadnessBlazor.Services;

public class TournamentDataService(HttpClient http)
{
    public static readonly int[] AvailableYears = [2015, 2016, 2017, 2018, 2019, 2021, 2022, 2023, 2024, 2025, 2026]; // 2020: no tournament

    private readonly Dictionary<int, TournamentYear> _cache = [];

    public async Task<TournamentYear?> LoadAsync(int year)
    {
        if (_cache.TryGetValue(year, out var cached)) return cached;

        TournamentYearDto? dto;
        try
        {
            dto = await http.GetFromJsonAsync<TournamentYearDto>($"data/tournament-{year}.json");
        }
        catch
        {
            return null;
        }
        if (dto == null) return null;

        var result = dto.ToModel();
        _cache[year] = result;
        return result;
    }
}

// ── JSON DTOs ────────────────────────────────────────────────────────────────
// Mirrors the schema written by the Python fetch script.

file class TournamentYearDto
{
    public int Year { get; init; }
    public TeamDto[] Teams { get; init; } = [];
    // results[round][position] = team id (0-based, matching Teams array index)
    public int?[][] Results { get; init; } = [];
    // scores[round][position] = [winnerScore, loserScore]  (null entries allowed)
    public int[]?[][] Scores { get; init; } = [];

    public TournamentYear ToModel()
    {
        var teams = Teams.Select((t, i) => new Team(
            Id:     i,
            Name:   t.Name,
            Seed:   t.Seed,
            Region: t.Region,
            Stats:  t.Stats == null ? null : new TeamStats(
                Pace:       t.Stats.Pace,
                ToPct:      t.Stats.ToPct,
                ToDefPct:   t.Stats.ToDefPct,
                OrPct:      t.Stats.OrPct,
                DrPct:      t.Stats.DrPct,
                ThreeRate:  t.Stats.ThreeRate,
                ThreeRateD: t.Stats.ThreeRateD,
                ThreePct:   t.Stats.ThreePct,
                ThreePctD:  t.Stats.ThreePctD,
                TwoPct:     t.Stats.TwoPct,
                TwoPctD:    t.Stats.TwoPctD,
                Ftr:        t.Stats.Ftr,
                FtrDef:     t.Stats.FtrDef,
                FtPct:      t.Stats.FtPct
            )
        )).ToArray();

        // Build result arrays — same jagged layout as BracketState (index 0 unused).
        var actualResults = new int?[7][];
        var scores        = new GameScore?[7][];
        for (int r = 0; r <= 6; r++)
        {
            int size = 64 >> r;
            actualResults[r] = new int?[size];
            scores[r]        = new GameScore?[size];
        }

        for (int r = 1; r <= 6; r++)
        {
            if (r - 1 >= Results.Length) break;
            var roundResults = Results[r - 1];
            var roundScores  = r - 1 < Scores.Length ? Scores[r - 1] : null;
            int count = 64 >> r;
            for (int p = 0; p < count && p < roundResults.Length; p++)
            {
                actualResults[r][p] = roundResults[p];
                if (roundScores != null && p < roundScores.Length && roundScores[p] != null)
                    scores[r][p] = new GameScore(roundScores[p]![0], roundScores[p]![1]);
            }
        }

        return new TournamentYear
        {
            Year          = Year,
            Teams         = teams,
            ActualResults = actualResults,
            Scores        = scores,
        };
    }
}

file class TeamDto
{
    public string Name   { get; init; } = "";
    public int    Seed   { get; init; }
    public string Region { get; init; } = "";
    public StatsDto? Stats { get; init; }
}

file class StatsDto
{
    public double Pace       { get; init; }
    public double ToPct      { get; init; }
    public double ToDefPct   { get; init; }
    public double OrPct      { get; init; }
    public double DrPct      { get; init; }
    public double ThreeRate  { get; init; }
    public double ThreeRateD { get; init; }
    public double ThreePct   { get; init; }
    public double ThreePctD  { get; init; }
    public double TwoPct     { get; init; }
    public double TwoPctD    { get; init; }
    public double Ftr        { get; init; }
    public double FtrDef     { get; init; }
    public double FtPct      { get; init; }
}
