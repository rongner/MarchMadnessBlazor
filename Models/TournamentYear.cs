namespace MarchMadnessBlazor.Models;

// Full historical tournament — bracket, stats, and actual results.
public class TournamentYear
{
    public int Year { get; init; }

    // 64 teams in bracket slot order (same layout as TournamentData.GetInitialSeeding).
    // Slots 0-15 = East, 16-31 = South, 32-47 = West, 48-63 = Midwest.
    // Within each region: 1v16, 8v9, 5v12, 4v13, 6v11, 3v14, 7v10, 2v15.
    public Team[] Teams { get; init; } = [];

    // [round][position] = team Id of the actual winner (null = game not played / data missing).
    // Round 1 = 32 games … Round 6 = Championship.
    public int?[][] ActualResults { get; init; } = [];

    // [round][position] = scores for that game (null if not available).
    public GameScore?[][] Scores { get; init; } = [];
}

public record GameScore(int WinnerScore, int LoserScore);
