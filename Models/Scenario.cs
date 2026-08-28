namespace StockInvestment.Models;

public sealed class Scenario
{
	public int Id { get; init; }
	public string Name { get; init; } = string.Empty;
	public int? Horizon { get; init; }
	public List<ScenarioAsset> Assets { get; init; } = [];
	public DateTime? Created { get; init; }
	
}

public sealed class ScenarioAsset
{
	public int Id { get; init; }
	public int ScenarioId { get; init; }
	public string Ticker { get; init; } = string.Empty;
	public decimal Invested { get; init; }
	public decimal Value { get; init; }
}