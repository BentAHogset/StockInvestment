using Microsoft.AspNetCore.Mvc;
using StockInvestment.Mcp;
using StockInvestment.Services;

namespace StockInvestment.Controllers;

[ApiController]
[Route("api")]
public sealed class InvestmentController : ControllerBase
{
	private readonly StockScannerTools _stockScannerTools;
	private readonly YahooFinanceQuoteService _yahooQuoteService;

	public InvestmentController(StockScannerTools stockScannerTools, YahooFinanceQuoteService yahooQuoteService)
	{
		_stockScannerTools = stockScannerTools;
		_yahooQuoteService = yahooQuoteService;
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

	[HttpGet("history/{symbol}")]
	public async Task<IActionResult> GetHistory(string symbol, CancellationToken cancellationToken = default)
	{
		var history = await _yahooQuoteService.GetHistoryAsync(symbol, cancellationToken);
		return history is null ? NotFound() : Ok(history);
	}
}
