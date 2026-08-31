using Microsoft.AspNetCore.Mvc;
using StockInvestment.Models;
using StockInvestment.Services;

namespace StockInvestment.Controllers;

[ApiController]
[Route("api/scenarios")]
public sealed class ScenariosController : ControllerBase
{
	private readonly ScenarioRepository _repository;

	public ScenariosController(ScenarioRepository repository)
	{
		_repository = repository;
	}

	[HttpGet]
	public async Task<IActionResult> GetScenarios(CancellationToken cancellationToken)
		=> Ok(await _repository.GetAllAsync(cancellationToken));

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetScenario(int id, CancellationToken cancellationToken)
	{
		var scenario = await _repository.GetAsync(id, cancellationToken);
		return scenario is null ? NotFound() : Ok(scenario);
	}

	[HttpPost]
	public async Task<IActionResult> CreateScenario([FromBody] CreateScenarioRequest request, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
			ModelState.AddModelError(nameof(request.Name), "Name is required and must be 100 characters or fewer.");

		if (request.Years is < 1 or > 50)
			ModelState.AddModelError(nameof(request.Years), "Years must be between 1 and 50.");

		if (request.Assets is null or { Count: 0 })
			ModelState.AddModelError(nameof(request.Assets), "At least one asset is required.");

		if (!ModelState.IsValid)
			return ValidationProblem(ModelState);

		var assets = request.Assets!
			.Where(asset => !string.IsNullOrWhiteSpace(asset.Ticker))
			.Select(asset => new ScenarioAsset
			{
				Ticker = asset.Ticker.Trim().ToUpperInvariant(),
				Invested = asset.InvestedAmount,
				Value = asset.ValueAmount
			})
			.ToArray();

		if (assets.Length == 0 || assets.Any(asset => asset.Invested < 0 || asset.Value < 0))
			return BadRequest("Assets must contain a ticker and non-negative amounts.");

		var scenario = await _repository.SaveAsync(request.Name.Trim(), request.Years, assets, cancellationToken);
		return CreatedAtAction(nameof(GetScenario), new { id = scenario.Id }, scenario);
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> DeleteScenario(int id, CancellationToken cancellationToken)
		=> await _repository.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}

public sealed class CreateScenarioRequest
{
	public string Name { get; init; } = string.Empty;
	public int Years { get; init; }
	public IReadOnlyList<CreateScenarioAssetRequest>? Assets { get; init; }
}

public sealed class CreateScenarioAssetRequest
{
	public string Ticker { get; init; } = string.Empty;
	public decimal InvestedAmount { get; init; }
	public decimal ValueAmount { get; init; }
}