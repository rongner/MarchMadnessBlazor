namespace MarchMadnessBlazor.Models;

public class BracketState
{
    // [round][position] — round 0 is the initial 64-team seeding (read-only).
    // Rounds 1-6 hold the winner of game `position` in that round.
    public Team?[][] UserPicks { get; }
    public Team?[][] SimResults { get; }
    public bool IsSimulated { get; set; }

    public static readonly int[] PointsPerRound = { 0, 10, 20, 40, 80, 160, 320 };
    public static readonly string[] RoundNames = { "", "Round of 64", "Round of 32", "Sweet 16", "Elite 8", "Final Four", "Championship" };

    public BracketState(Team[] initialSeeding)
    {
        UserPicks = new Team?[7][];
        SimResults = new Team?[7][];
        for (int r = 0; r <= 6; r++)
        {
            int size = 64 >> r;
            UserPicks[r] = new Team?[size];
            SimResults[r] = new Team?[size];
        }
        Array.Copy(initialSeeding, UserPicks[0], 64);
        Array.Copy(initialSeeding, SimResults[0], 64);
    }

    // Returns the two teams playing in game `position` of `round`.
    public (Team? Top, Team? Bottom) GetGameTeams(Team?[][] bracket, int round, int position) =>
        (bracket[round - 1][position * 2], bracket[round - 1][position * 2 + 1]);

    public void SetUserPick(int round, int position, Team team)
    {
        UserPicks[round][position] = team;
        // Clear any downstream picks that are no longer valid.
        for (int r = round + 1; r <= 6; r++)
        {
            int count = 64 >> r;
            for (int p = 0; p < count; p++)
            {
                if (UserPicks[r][p] == null) continue;
                var (top, bottom) = GetGameTeams(UserPicks, r, p);
                if (UserPicks[r][p] != top && UserPicks[r][p] != bottom)
                    UserPicks[r][p] = null;
            }
        }
    }

    public PickResult GetPickResult(int round, int position)
    {
        if (!IsSimulated) return PickResult.None;
        var user = UserPicks[round][position];
        var sim  = SimResults[round][position];
        if (user == null || sim == null) return PickResult.None;
        return user == sim ? PickResult.Correct : PickResult.Wrong;
    }

    public int TotalScore()
    {
        if (!IsSimulated) return 0;
        int score = 0;
        for (int r = 1; r <= 6; r++)
        {
            int count = 64 >> r;
            for (int p = 0; p < count; p++)
                if (UserPicks[r][p] != null && UserPicks[r][p] == SimResults[r][p])
                    score += PointsPerRound[r];
        }
        return score;
    }

    public int RoundScore(int round)
    {
        if (!IsSimulated) return 0;
        int count = 64 >> round;
        int score = 0;
        for (int p = 0; p < count; p++)
            if (UserPicks[round][p] != null && UserPicks[round][p] == SimResults[round][p])
                score += PointsPerRound[round];
        return score;
    }

    public int PicksComplete()
    {
        int count = 0;
        for (int r = 1; r <= 6; r++)
        {
            int games = 64 >> r;
            for (int p = 0; p < games; p++)
                if (UserPicks[r][p] != null) count++;
        }
        return count;
    }
}

public enum PickResult { None, Correct, Wrong }
