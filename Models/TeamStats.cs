namespace MarchMadnessBlazor.Models;

// All rate stats are percentages expressed as 0–100 (e.g. TO% = 14.2, not 0.142).
// FTR = FTA/FGA × 100 (Barttorvik convention).
// Pace = possessions per 40 minutes (WarrenNolan convention).
public record TeamStats(
    double Pace,
    double ToPct,      // Turnover %
    double ToDefPct,   // Forced turnover % (opponent TO% against this team)
    double OrPct,      // Offensive rebound %
    double DrPct,      // Defensive rebound %
    double ThreeRate,  // 3-point attempt rate (% of possessions ending in 3PA)
    double ThreeRateD, // Opponent 3-point attempt rate allowed
    double ThreePct,   // 3-point make %
    double ThreePctD,  // Opponent 3-point make % allowed
    double TwoPct,     // 2-point make %
    double TwoPctD,    // Opponent 2-point make % allowed
    double Ftr,        // Free throw rate (FTA/FGA × 100)
    double FtrDef,     // Opponent free throw rate allowed
    double FtPct       // Free throw % (no opponent adjustment)
);
