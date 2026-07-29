using System.Globalization;
using StockInvestment.Models;

namespace StockInvestment.Services;

public sealed class StooqQuoteService
{
    private readonly HttpClient _httpClient;

    public StooqQuoteService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StockQuote?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        var normalizedSymbol = symbol.Trim().ToLowerInvariant();
        if (!normalizedSymbol.Contains('.'))
        {
            normalizedSymbol = $"{normalizedSymbol}.us";
        }

        using var response = await _httpClient.GetAsync($"q/l/?s={normalizedSymbol}&f=sd2t2ohlcvn&i=d", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var csv = await response.Content.ReadAsStringAsync(cancellationToken);
        var lines = csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            return null;
        }

        var parts = lines[1].Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 9)
        {
            return null;
        }

        var close = ParseDecimal(parts[6]);
        if (close is null)
        {
            return null;
        }

        var open = ParseDecimal(parts[3]);
        var high = ParseDecimal(parts[4]);
        var low = ParseDecimal(parts[5]);
        decimal? previousClose = null;
        decimal? dailyChangePercent = open is > 0
            ? Math.Round(((close.Value - open.Value) / open.Value) * 100m, 2)
            : null;

        DateOnly? asOfDate = null;
        if (DateOnly.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            asOfDate = parsedDate;
        }

        return new StockQuote(
            Symbol: parts[0],
            LastPrice: close.Value,
            Open: open,
            High: high,
            Low: low,
            PreviousClose: previousClose,
            DailyChangePercent: dailyChangePercent,
            AsOfDate: asOfDate,
            IsDelayed: true
        );
    }

    private static decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/D")
        {
            return null;
        }

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
