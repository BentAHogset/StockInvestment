using System.Text.Json;
using StockInvestment.Models;

namespace StockInvestment.Services;

public sealed class YahooFinanceQuoteService
{
    private readonly HttpClient _httpClient;

    public YahooFinanceQuoteService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyDictionary<string, MarketSnapshot>> GetSnapshotsAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default)
    {
        var symbolList = symbols
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (symbolList.Length == 0)
        {
            return new Dictionary<string, MarketSnapshot>(StringComparer.OrdinalIgnoreCase);
        }

        var snapshots = new Dictionary<string, MarketSnapshot>(StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in symbolList.Chunk(20))
        {
            var query = string.Join(',', chunk);
            using var response = await _httpClient.GetAsync($"v7/finance/spark?symbols={query}&range=1mo&interval=1d", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("spark", out var spark) ||
                !spark.TryGetProperty("result", out var result) ||
                result.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in result.EnumerateArray())
            {
                if (!item.TryGetProperty("symbol", out var symbolElement))
                {
                    continue;
                }

                var symbol = symbolElement.GetString();
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    continue;
                }

                if (!item.TryGetProperty("response", out var responseArray) || responseArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var responseNode = responseArray.EnumerateArray().FirstOrDefault();
                if (responseNode.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                decimal? lastPrice = null;
                if (responseNode.TryGetProperty("meta", out var metaNode) &&
                    metaNode.TryGetProperty("regularMarketPrice", out var priceNode) &&
                    priceNode.TryGetDecimal(out var parsedPrice))
                {
                    lastPrice = parsedPrice;
                }

                var closes = ExtractCloseSeries(responseNode);
                if (closes.Count < 2)
                {
                    snapshots[symbol] = new MarketSnapshot(symbol, lastPrice, 0m, 0m);
                    continue;
                }

                var first = closes.First();
                var last = closes.Last();
                var oneMonthReturn = first > 0m
                    ? ((last - first) / first) * 100m
                    : 0m;

                var dailyReturns = new List<decimal>();
                for (var i = 1; i < closes.Count; i++)
                {
                    var prev = closes[i - 1];
                    var current = closes[i];
                    if (prev <= 0m)
                    {
                        continue;
                    }

                    dailyReturns.Add((current - prev) / prev);
                }

                var volatilityPercent = CalculateStdDev(dailyReturns) * 100m;

                snapshots[symbol] = new MarketSnapshot(
                    Symbol: symbol,
                    LastPrice: lastPrice,
                    OneMonthReturnPercent: Math.Round(oneMonthReturn, 2),
                    VolatilityPercent: Math.Round(volatilityPercent, 2)
                );
            }
        }

        return snapshots;
    }

    public async Task<decimal?> GetAnalystTargetAsync(string symbol, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"v10/finance/quoteSummary/{Uri.EscapeDataString(symbol)}?modules=financialData",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("quoteSummary", out var quoteSummary) ||
            !quoteSummary.TryGetProperty("result", out var resultArray) ||
            resultArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var resultNode = resultArray.EnumerateArray().FirstOrDefault();
        if (resultNode.ValueKind != JsonValueKind.Object ||
            !resultNode.TryGetProperty("financialData", out var financialData) ||
            !financialData.TryGetProperty("targetMeanPrice", out var targetNode) ||
            !targetNode.TryGetProperty("raw", out var rawNode) ||
            !rawNode.TryGetDecimal(out var targetMeanPrice))
        {
            return null;
        }

        return targetMeanPrice;
    }

    public async Task<StockHistory?> GetHistoryAsync(string symbol, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"v8/finance/chart/{Uri.EscapeDataString(symbol)}?range=1y&interval=1d",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("chart", out var chart) ||
            !chart.TryGetProperty("result", out var resultArray) ||
            resultArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var resultNode = resultArray.EnumerateArray().FirstOrDefault();
        if (resultNode.ValueKind != JsonValueKind.Object ||
            !resultNode.TryGetProperty("timestamp", out var timestampArray) ||
            timestampArray.ValueKind != JsonValueKind.Array ||
            !resultNode.TryGetProperty("indicators", out var indicatorsNode) ||
            !indicatorsNode.TryGetProperty("quote", out var quoteArray) ||
            quoteArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var quoteNode = quoteArray.EnumerateArray().FirstOrDefault();
        if (quoteNode.ValueKind != JsonValueKind.Object ||
            !quoteNode.TryGetProperty("close", out var closeArray) ||
            closeArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var timestamps = timestampArray.EnumerateArray().ToArray();
        var closes = closeArray.EnumerateArray().ToArray();
        var points = new List<StockHistoryPoint>(timestamps.Length);

        for (var i = 0; i < timestamps.Length && i < closes.Length; i++)
        {
            if (!timestamps[i].TryGetInt64(out var unixSeconds) ||
                closes[i].ValueKind != JsonValueKind.Number ||
                !closes[i].TryGetDecimal(out var close))
            {
                continue;
            }

            points.Add(new StockHistoryPoint(DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime, close));
        }

        decimal? oneYearReturn = points.Count >= 2 && points[0].Close > 0m
            ? Math.Round(((points[^1].Close - points[0].Close) / points[0].Close) * 100m, 2)
            : null;

        return new StockHistory(symbol, oneYearReturn, points);
    }

    private static List<decimal> ExtractCloseSeries(JsonElement responseNode)
    {
        var closes = new List<decimal>();

        if (!responseNode.TryGetProperty("indicators", out var indicatorsNode) ||
            !indicatorsNode.TryGetProperty("quote", out var quoteArray) ||
            quoteArray.ValueKind != JsonValueKind.Array)
        {
            return closes;
        }

        var quoteNode = quoteArray.EnumerateArray().FirstOrDefault();
        if (quoteNode.ValueKind != JsonValueKind.Object ||
            !quoteNode.TryGetProperty("close", out var closeArray) ||
            closeArray.ValueKind != JsonValueKind.Array)
        {
            return closes;
        }

        foreach (var close in closeArray.EnumerateArray())
        {
            if (close.ValueKind == JsonValueKind.Number && close.TryGetDecimal(out var value))
            {
                closes.Add(value);
            }
        }

        return closes;
    }

    private static decimal CalculateStdDev(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return 0m;
        }

        var mean = values.Average();
        var variance = values.Select(v => (v - mean) * (v - mean)).Average();
        return (decimal)Math.Sqrt((double)variance);
    }
}
