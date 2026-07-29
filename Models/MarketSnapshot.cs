namespace StockInvestment.Models;

public sealed record MarketSnapshot(
    string Symbol,
    decimal? LastPrice,
    decimal OneMonthReturnPercent,
    decimal VolatilityPercent
);
