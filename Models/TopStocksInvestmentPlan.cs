namespace StockInvestment.Models;

public sealed record TopStocksInvestmentPlan(
    decimal MonthlyBudget,
    int StockCount,
    bool LiveDataAvailable,
    IReadOnlyList<TopStockRecommendation> TopStocks,
    IReadOnlyList<InvestmentAllocation> Allocation,
    string Guidance
);
