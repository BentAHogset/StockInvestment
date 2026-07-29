namespace StockInvestment.Models;

public sealed record StockQuote(
    string Symbol,
    decimal LastPrice,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal? PreviousClose,
    decimal? DailyChangePercent,
    DateOnly? AsOfDate,
    bool IsDelayed
);
