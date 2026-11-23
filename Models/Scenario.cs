namespace StockInvestment.Models;

public sealed class Scenario
{
	public int Id { get; init; }
	public string Name { get; set; } = string.Empty;
	public int? Horizon { get; set; }
	public List<ScenarioAsset> Assets { get; set; } = [];
	public DateTime? Created { get; init; }

}

public sealed class ScenarioAsset
{
	public int Id { get; init; }
	public int ScenarioId { get; set; }
	public string Ticker { get; set; } = string.Empty;
	public decimal Invested { get; set; }
	public decimal Value { get; set; }
}