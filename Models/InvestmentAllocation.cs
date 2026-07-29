namespace StockInvestment.Models;

public sealed record InvestmentAllocation(
    string Symbol,
    decimal WeightPercent,
    decimal MonthlyAmount,
    string Strategy
);
