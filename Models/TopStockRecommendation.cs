namespace StockInvestment.Models;

public sealed record TopStockRecommendation(
    string Symbol,
    int ReliabilityScore,
    decimal? LastPrice,
    decimal? DailyChangePercent,
    bool UsesLiveQuote,
    string Reason,
    decimal? TargetMeanPrice = null,
    decimal? EstimatedAnnualReturnPercent = null
);
