namespace MarchMadnessBlazor.Models;

public record Team(int Id, string Name, int Seed, string Region, TeamStats? Stats = null);
