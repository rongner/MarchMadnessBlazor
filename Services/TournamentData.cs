using MarchMadnessBlazor.Models;

namespace MarchMadnessBlazor.Services;

public static class TournamentData
{
    // Regions: 0=East, 1=South, 2=West, 3=Midwest
    // East+South are the left bracket half; West+Midwest are the right half.
    // FF Game 0: East champion vs South champion
    // FF Game 1: West champion vs Midwest champion
    //
    // Within each region, teams are listed in first-round matchup order:
    //   1v16, 8v9, 5v12, 4v13, 6v11, 3v14, 7v10, 2v15

    private static readonly (string Name, int Seed, string Region)[] RegionData =
    {
        // East (slots 0-15) — 2024 NCAA tournament
        ("UConn",            1,  "East"),
        ("Stetson",         16,  "East"),
        ("FAU",              8,  "East"),
        ("Northwestern",     9,  "East"),
        ("San Diego State",  5,  "East"),
        ("UAB",             12,  "East"),
        ("Auburn",           4,  "East"),
        ("Yale",            13,  "East"),
        ("BYU",              6,  "East"),
        ("Duquesne",        11,  "East"),
        ("Illinois",         3,  "East"),
        ("Morehead State",  14,  "East"),
        ("Washington State", 7,  "East"),
        ("Drake",           10,  "East"),
        ("Iowa State",       2,  "East"),
        ("South Dakota St.", 15, "East"),

        // South (slots 16-31)
        ("Houston",          1,  "South"),
        ("Longwood",        16,  "South"),
        ("Nebraska",         8,  "South"),
        ("Texas A&M",        9,  "South"),
        ("Wisconsin",        5,  "South"),
        ("James Madison",   12,  "South"),
        ("Duke",             4,  "South"),
        ("Vermont",         13,  "South"),
        ("Texas Tech",       6,  "South"),
        ("NC State",        11,  "South"),
        ("Kentucky",         3,  "South"),
        ("Akron",           14,  "South"),
        ("Florida",          7,  "South"),
        ("Colorado",        10,  "South"),
        ("Marquette",        2,  "South"),
        ("Long Island Univ.",15, "South"),

        // West (slots 32-47)
        ("North Carolina",   1,  "West"),
        ("Wagner",          16,  "West"),
        ("Mississippi State",8,  "West"),
        ("Michigan State",   9,  "West"),
        ("Saint Mary's",     5,  "West"),
        ("Grand Canyon",    12,  "West"),
        ("Alabama",          4,  "West"),
        ("Charleston",      13,  "West"),
        ("Clemson",          6,  "West"),
        ("New Mexico",      11,  "West"),
        ("Baylor",           3,  "West"),
        ("Colgate",         14,  "West"),
        ("Dayton",           7,  "West"),
        ("Nevada",          10,  "West"),
        ("Arizona",          2,  "West"),
        ("Long Beach State", 15, "West"),

        // Midwest (slots 48-63)
        ("Purdue",           1,  "Midwest"),
        ("Grambling",       16,  "Midwest"),
        ("Utah State",       8,  "Midwest"),
        ("TCU",              9,  "Midwest"),
        ("Gonzaga",          5,  "Midwest"),
        ("McNeese",         12,  "Midwest"),
        ("Kansas",           4,  "Midwest"),
        ("Samford",         13,  "Midwest"),
        ("South Carolina",   6,  "Midwest"),
        ("Oregon",          11,  "Midwest"),
        ("Creighton",        3,  "Midwest"),
        ("Montana State",   14,  "Midwest"),
        ("Texas",            7,  "Midwest"),
        ("Colorado State",  10,  "Midwest"),
        ("Tennessee",        2,  "Midwest"),
        ("Saint Peter's",   15,  "Midwest"),
    };

    public static Team[] GetInitialSeeding() =>
        RegionData.Select((t, i) => new Team(i, t.Name, t.Seed, t.Region)).ToArray();
}
