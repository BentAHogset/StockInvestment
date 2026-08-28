using Microsoft.AspNetCore.Mvc;
using StockInvestment.Mcp;

namespace StockInvestment.Controllers;

[ApiController]
[Route("api")]
public sealed class InvestmentController : ControllerBase
{
	private readonly StockScannerTools _stockScannerTools;

	public InvestmentController(StockScannerTools stockScannerTools)
	{
		_stockScannerTools = stockScannerTools;
	}

	[HttpGet("plan")]
	public async Task<IActionResult> GetPlan(
		[FromQuery] int topCount = 10,
		[FromQuery] string? strategy = null,
		CancellationToken cancellationToken = default)
	{
		const decimal monthlyBudget = 200m;
		var useAggressive = string.IsNullOrWhiteSpace(strategy) ||
			strategy.Equals("aggressive", StringComparison.OrdinalIgnoreCase);

		var plan = useAggressive
			? await _stockScannerTools.CalculateDynamicOpportunitiesAsync(monthlyBudget, topCount, cancellationToken)
			: await _stockScannerTools.CalculateTopStocksPlanAsync(monthlyBudget, topCount, cancellationToken);

		return Ok(new
		{
			Service = "StockInvestment",
			McpEndpoint = "/mcp",
			ToolsUsed = useAggressive
				? new[] { "calculate_dynamic_opportunities" }
				: new[] { "scan_stocks", "calculate_top_stocks_plan" },
			Input = new
			{
				TopCount = Math.Clamp(topCount, 3, 10),
				Strategy = useAggressive ? "aggressive" : "reliable"
			},
			Plan = plan
		});
	}
}
