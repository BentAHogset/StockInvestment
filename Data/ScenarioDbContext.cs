using Microsoft.EntityFrameworkCore;
using StockInvestment.Models;

namespace StockInvestment.Data;

public sealed class ScenarioDbContext : DbContext
{
	public ScenarioDbContext(DbContextOptions<ScenarioDbContext> options)
		: base(options)
	{
	}

	public DbSet<Scenario> Scenarios => Set<Scenario>();
	public DbSet<ScenarioAsset> ScenarioAssets => Set<ScenarioAsset>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Scenario>(entity =>
		{
			entity.ToTable("Scenario");
			entity.HasKey(scenario => scenario.Id);
			entity.Property(scenario => scenario.Id).UseIdentityColumn();
			entity.Property(scenario => scenario.Name).HasMaxLength(100).IsRequired();
			entity.HasMany(scenario => scenario.Assets)
				.WithOne()
				.HasForeignKey(asset => asset.ScenarioId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ScenarioAsset>(entity =>
		{
			entity.ToTable("Asset");
			entity.HasKey(asset => asset.Id);
			entity.Property(asset => asset.Id).UseIdentityColumn();
			entity.Property(asset => asset.Ticker).HasMaxLength(20).IsRequired();
			entity.Property(asset => asset.Invested).HasPrecision(18, 2);
			entity.Property(asset => asset.Value).HasPrecision(18, 2);
		});
	}
}