namespace StockInvestment.Models;

public sealed record StockHistoryPoint(DateTime Date, decimal Close);

public sealed record StockHistory(
    string Symbol,
    decimal? OneYearReturnPercent,
    IReadOnlyList<StockHistoryPoint> Points
);
