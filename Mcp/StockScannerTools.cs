using System.ComponentModel;
using ModelContextProtocol.Server;
using StockInvestment.Models;
using StockInvestment.Services;

namespace StockInvestment.Mcp;

[McpServerToolType]
public sealed class StockScannerTools
{
    private readonly StooqQuoteService _quoteService;
    private readonly YahooFinanceQuoteService _yahooQuoteService;

    private static readonly (string Symbol, int BaseReliability, string Reason)[] ReliableCandidates =
    [
        ("MSFT", 95, "Cloud and enterprise cash-flow durability"),
        ("AAPL", 94, "Strong ecosystem and pricing power"),
        ("GOOGL", 93, "Search dominance and AI infrastructure scale"),
        ("AMZN", 92, "Diversified commerce plus AWS profits"),
        ("NVDA", 92, "AI platform leadership and demand visibility"),
        ("BRK.B", 91, "Multi-industry resilience and capital allocation"),
        ("V", 90, "Global payments moat and high margins"),
        ("COST", 89, "Membership model and stable demand"),
        ("JNJ", 88, "Defensive healthcare cash generation"),
        ("PG", 87, "Consumer staples stability across cycles"),
        ("JPM", 86, "Scale banking franchise and balance sheet quality"),
        ("ADBE", 85, "Recurring software revenue and retention"),
        ("ORCL", 84, "Mission-critical enterprise software base"),
        ("QCOM", 83, "Licensing and semiconductor cash flow mix"),
        ("CRM", 82, "Enterprise software breadth and recurring demand")
    ];

    private static readonly (string Symbol, string Theme, int ControversyBonus)[] AggressiveUniverse =
    [
        ("COIN", "Crypto exchange exposure", 10),
        ("MSTR", "Bitcoin treasury leverage", 10),
        ("MARA", "Bitcoin mining sensitivity", 9),
        ("RIOT", "Crypto mining volatility", 9),
        ("PLTR", "Defense and surveillance analytics", 7),
        ("LMT", "Defense spending exposure", 7),
        ("NOC", "Aerospace and defense systems", 7),
        ("BA", "Aerospace turnaround risk", 6),
        ("MO", "Tobacco cashflow profile", 10),
        ("BTI", "Global tobacco yield profile", 10),
        ("PM", "Nicotine products demand", 9),
        ("DKNG", "Online gambling and betting demand", 10),
        ("PENN", "Sports betting and casino risk", 10),
        ("RBLX", "Consumer discretionary volatility", 6),
        ("SMCI", "High-beta AI infrastructure", 8),
        ("SOXL", "Leveraged semiconductor ETF", 10),
        ("TQQQ", "Leveraged technology ETF", 10),
        ("TSLA", "High-volatility growth and execution risk", 7),
        ("NVDA", "AI concentration momentum", 6),
        ("RKLB", "Space launch speculative growth", 8),
        ("HIMS", "Regulatory and growth sensitivity", 7),
        ("ASTS", "Satellite network speculative execution", 9),
        ("IONQ", "Early-stage quantum commercialization", 8),
        ("HOOD", "Retail trading cyclicality", 8),
        ("CELH", "Consumer growth momentum swings", 6)
    ];

    public StockScannerTools(StooqQuoteService quoteService, YahooFinanceQuoteService yahooQuoteService)
    {
        _quoteService = quoteService;
        _yahooQuoteService = yahooQuoteService;
    }

    [McpServerTool(Name = "scan_stocks"), Description("Scans symbols and returns quotes that match basic price and daily move filters.")]
    public async Task<IReadOnlyList<StockQuote>> ScanStocksAsync(
        [Description("Comma-separated symbols (for example: AAPL,MSFT,TSLA). Symbols without market suffix default to .US")]
        string symbols,
        [Description("Optional minimum last price filter")] decimal? minPrice = null,
        [Description("Optional maximum last price filter")] decimal? maxPrice = null,
        [Description("Optional minimum daily percent change filter")] decimal? minDailyChangePercent = null,
        CancellationToken cancellationToken = default)
    {
        var inputSymbols = symbols
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToArray();

        if (inputSymbols.Length == 0)
        {
            return Array.Empty<StockQuote>();
        }

        var results = new List<StockQuote>(inputSymbols.Length);
        foreach (var symbol in inputSymbols)
        {
            var quote = await _quoteService.GetLatestQuoteAsync(symbol, cancellationToken);
            if (quote is null)
            {
                continue;
            }

            if (minPrice.HasValue && quote.LastPrice < minPrice.Value)
            {
                continue;
            }

            if (maxPrice.HasValue && quote.LastPrice > maxPrice.Value)
            {
                continue;
            }

            if (minDailyChangePercent.HasValue)
            {
                var pct = quote.DailyChangePercent ?? decimal.MinValue;
                if (pct < minDailyChangePercent.Value)
                {
                    continue;
                }
            }

            results.Add(quote);
        }

        return results
            .OrderByDescending(q => q.DailyChangePercent ?? decimal.MinValue)
            .ThenBy(q => q.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    [McpServerTool(Name = "calculate_top_stocks_plan"), Description("Calculates top reliable stocks and a monthly investment allocation plan for a small-budget investor.")]
    public async Task<TopStocksInvestmentPlan> CalculateTopStocksPlanAsync(
        [Description("Monthly amount to invest, e.g. 200")]
        decimal monthlyBudget = 200m,
        [Description("How many top stocks to include, max 10")]
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedBudget = Math.Clamp(monthlyBudget, 25m, 100_000m);
        var normalizedTopCount = Math.Clamp(topCount, 3, 10);

        var symbols = ReliableCandidates.Select(c => c.Symbol).ToArray();
        var quotes = await ScanStocksAsync(string.Join(',', symbols), cancellationToken: cancellationToken);
        var quotesBySymbol = quotes.ToDictionary(q => q.Symbol.Split('.')[0].ToUpperInvariant(), q => q, StringComparer.OrdinalIgnoreCase);

        var ranked = ReliableCandidates
            .Select(c =>
            {
                quotesBySymbol.TryGetValue(c.Symbol, out var quote);

                var momentumAdjustment = quote?.DailyChangePercent is decimal pct
                    ? (int)Math.Clamp(Math.Round(pct), -3m, 3m)
                    : 0;

                var score = Math.Clamp(c.BaseReliability + momentumAdjustment, 1, 99);

                return new TopStockRecommendation(
                    Symbol: c.Symbol,
                    ReliabilityScore: score,
                    LastPrice: quote?.LastPrice,
                    DailyChangePercent: quote?.DailyChangePercent,
                    UsesLiveQuote: quote is not null,
                    Reason: c.Reason
                );
            })
            .OrderByDescending(s => s.ReliabilityScore)
            .ThenBy(s => s.Symbol, StringComparer.OrdinalIgnoreCase)
            .Take(normalizedTopCount)
            .ToArray();

        var rawWeights = Enumerable.Range(0, ranked.Length)
            .Select(i => Math.Max(1m, 12m - i))
            .ToArray();

        var totalWeight = rawWeights.Sum();
        var allocation = new List<InvestmentAllocation>(ranked.Length);
        decimal allocated = 0m;

        for (var i = 0; i < ranked.Length; i++)
        {
            var isLast = i == ranked.Length - 1;
            var weightPercent = Math.Round((rawWeights[i] / totalWeight) * 100m, 2);
            var target = isLast
                ? Math.Round(normalizedBudget - allocated, 2)
                : Math.Round((normalizedBudget * weightPercent) / 100m, 2);

            allocated += target;

            allocation.Add(new InvestmentAllocation(
                Symbol: ranked[i].Symbol,
                WeightPercent: weightPercent,
                MonthlyAmount: target,
                Strategy: i < 3 ? "Core compounder" : "Diversified quality"
            ));
        }

        return new TopStocksInvestmentPlan(
            MonthlyBudget: normalizedBudget,
            StockCount: ranked.Length,
            LiveDataAvailable: ranked.Any(r => r.UsesLiveQuote),
            TopStocks: ranked,
            Allocation: allocation,
            Guidance: "Invest monthly, rebalance quarterly, and increase contributions as income grows."
        );
    }

    [McpServerTool(Name = "calculate_dynamic_opportunities"), Description("Evaluates aggressive high-risk market opportunities and builds a monthly allocation plan.")]
    public async Task<TopStocksInvestmentPlan> CalculateDynamicOpportunitiesAsync(
        [Description("Monthly amount to invest, e.g. 300")] decimal monthlyBudget = 300m,
        [Description("How many top opportunities to include, max 10")] int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedBudget = Math.Clamp(monthlyBudget, 25m, 100_000m);
        var normalizedTopCount = Math.Clamp(topCount, 3, 10);

        var symbols = AggressiveUniverse.Select(c => c.Symbol).ToArray();
        var snapshots = await _yahooQuoteService.GetSnapshotsAsync(symbols, cancellationToken);

        var ranked = AggressiveUniverse
            .Select(candidate =>
            {
                snapshots.TryGetValue(candidate.Symbol, out var snapshot);

                var oneMonthReturn = snapshot?.OneMonthReturnPercent ?? 0m;
                var volatility = snapshot?.VolatilityPercent ?? 0m;

                var upsideMomentum = Math.Max(oneMonthReturn, 0m) * 0.9m;
                var reboundPotential = Math.Max(-oneMonthReturn, 0m) * 0.55m;
                var movementOpportunity = Math.Abs(oneMonthReturn) * 0.35m;
                var score = 45m + upsideMomentum + reboundPotential + movementOpportunity + (volatility * 1.4m) + candidate.ControversyBonus;
                var normalizedScore = (int)Math.Clamp(Math.Round(score), 1m, 99m);

                var reason = snapshot is null
                    ? $"{candidate.Theme}; live quote unavailable so score favors controversy/risk profile"
                    : $"{candidate.Theme}; 1M return {snapshot.OneMonthReturnPercent:0.00}% and volatility {snapshot.VolatilityPercent:0.00}%";

                return new TopStockRecommendation(
                    Symbol: candidate.Symbol,
                    ReliabilityScore: normalizedScore,
                    LastPrice: snapshot?.LastPrice,
                    DailyChangePercent: snapshot?.OneMonthReturnPercent,
                    UsesLiveQuote: snapshot is not null,
                    Reason: reason
                );
            })
            .OrderByDescending(s => s.ReliabilityScore)
            .ThenBy(s => s.Symbol, StringComparer.OrdinalIgnoreCase)
            .Take(normalizedTopCount)
            .ToArray();

        var scoreTotal = ranked.Sum(r => Math.Max(1, r.ReliabilityScore));
        var allocation = new List<InvestmentAllocation>(ranked.Length);
        decimal allocated = 0m;

        for (var i = 0; i < ranked.Length; i++)
        {
            var scoreWeight = Math.Max(1, ranked[i].ReliabilityScore);
            var weightPercent = Math.Round((scoreWeight / (decimal)scoreTotal) * 100m, 2);
            var isLast = i == ranked.Length - 1;
            var amount = isLast
                ? Math.Round(normalizedBudget - allocated, 2)
                : Math.Round((normalizedBudget * weightPercent) / 100m, 2);

            allocated += amount;

            allocation.Add(new InvestmentAllocation(
                Symbol: ranked[i].Symbol,
                WeightPercent: weightPercent,
                MonthlyAmount: amount,
                Strategy: "Aggressive / high-risk"
            ));
        }

        return new TopStocksInvestmentPlan(
            MonthlyBudget: normalizedBudget,
            StockCount: ranked.Length,
            LiveDataAvailable: ranked.Any(r => r.UsesLiveQuote),
            TopStocks: ranked,
            Allocation: allocation,
            Guidance: "High-risk strategy: extreme volatility is expected. Reassess monthly and cap losses with strict position sizing."
        );
    }
}
