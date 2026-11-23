using Microsoft.EntityFrameworkCore;
using StockInvestment.Data;
using StockInvestment.Models;

namespace StockInvestment.Services;

public sealed class ScenarioRepository
{
	private readonly ScenarioDbContext _dbContext;

	public ScenarioRepository(ScenarioDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public Task<List<Scenario>> GetAllAsync(CancellationToken cancellationToken = default) =>
		_dbContext.Scenarios
			.AsNoTracking()
			.Include(scenario => scenario.Assets)
			.OrderByDescending(scenario => scenario.Created)
			.ToListAsync(cancellationToken);

	public Task<Scenario?> GetAsync(int id, CancellationToken cancellationToken = default) =>
		_dbContext.Scenarios
			.AsNoTracking()
			.Include(scenario => scenario.Assets)
			.SingleOrDefaultAsync(scenario => scenario.Id == id, cancellationToken);

	public async Task<Scenario> SaveAsync(string name, int years, IReadOnlyList<ScenarioAsset> assets, CancellationToken cancellationToken = default)
	{
		var now = DateTime.UtcNow;
		var scenario = new Scenario
		{
			Name = name,
			Horizon = years,
			Assets = assets.Select(asset => new ScenarioAsset
			{
				Ticker = asset.Ticker,
				Invested = asset.Invested,
				Value = asset.Value
			}).ToList(),
			Created = now
		};

		_dbContext.Scenarios.Add(scenario);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return scenario;
	}

	public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
	{
		var scenario = await _dbContext.Scenarios.FindAsync([id], cancellationToken);
		if (scenario is null)
			return false;

		_dbContext.Scenarios.Remove(scenario);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return true;
	}

	public async Task<Scenario?> UpdateAsync(int id, string name, int years, IReadOnlyList<ScenarioAsset> assets, CancellationToken cancellationToken = default)
	{
		var scenario = await _dbContext.Scenarios
			.Include(item => item.Assets)
			.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

		if (scenario is null)
			return null;

		scenario.Name = name;
		scenario.Horizon = years;

		var existingAssets = scenario.Assets.ToList();
		foreach (var existing in existingAssets)
		{
			var replacement = assets.FirstOrDefault(asset =>
				string.Equals(asset.Ticker, existing.Ticker, StringComparison.OrdinalIgnoreCase));

			if (replacement is null)
			{
				_dbContext.ScenarioAssets.Remove(existing);
				continue;
			}

			existing.Invested = replacement.Invested;
			existing.Value = replacement.Value;
		}

		var existingTickers = existingAssets
			.Select(asset => asset.Ticker)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		foreach (var asset in assets.Where(asset => !existingTickers.Contains(asset.Ticker)))
		{
			scenario.Assets.Add(new ScenarioAsset
			{
				Ticker = asset.Ticker,
				Invested = asset.Invested,
				Value = asset.Value,
				ScenarioId = scenario.Id
			});
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		return scenario;
	}
}