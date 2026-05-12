namespace MarchMadnessBlazor.Models;

public enum SimMode { Random, Seeded, Chalk, Stats }

public record SimulationSettings
{
    public SimMode     Mode          { get; init; } = SimMode.Seeded;
    public double      SeedInfluence { get; init; } = 1.0;
    public StatWeights Weights       { get; init; } = new();
}

public record StatWeights
{
    public double Pace          { get; init; } = 1.0;
    public double Turnovers     { get; init; } = 1.0;
    public double Rebounding    { get; init; } = 1.0;
    public double ThreeRate     { get; init; } = 1.0;
    public double ThreePct      { get; init; } = 1.0;
    public double TwoPct        { get; init; } = 1.0;
    public double FreeThrowRate { get; init; } = 1.0;
    public double FreeThrowPct  { get; init; } = 1.0;
}
