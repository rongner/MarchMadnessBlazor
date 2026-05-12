namespace MarchMadnessBlazor.Models;

public enum SimMode { Random, Seeded, Chalk }

public record SimulationSettings
{
    public SimMode Mode { get; init; } = SimMode.Seeded;
    public double SeedInfluence { get; init; } = 1.0;
}
