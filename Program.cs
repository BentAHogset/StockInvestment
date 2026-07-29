using ModelContextProtocol.AspNetCore;
using StockInvestment.Mcp;
using StockInvestment.Models;
using StockInvestment.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddMcpServer()
	.WithHttpTransport()
	.WithTools<StockScannerTools>();

builder.Services.AddTransient<StockScannerTools>();

builder.Services.AddHttpClient<StooqQuoteService>((serviceProvider, client) =>
{
	var configuration = serviceProvider.GetRequiredService<IConfiguration>();
	var section = configuration.GetSection("StockScanner:Stooq");

	var timeoutSeconds = section.GetValue<int?>("RequestTimeoutSeconds") ?? 10;
	var userAgent = section.GetValue<string>("UserAgent") ?? "StockInvestmentScanner/1.0";

	client.BaseAddress = new Uri("https://stooq.com/");
	client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 2, 30));
	client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
});

builder.Services.AddHttpClient<YahooFinanceQuoteService>((serviceProvider, client) =>
{
	var configuration = serviceProvider.GetRequiredService<IConfiguration>();
	var timeoutSeconds = configuration.GetValue<int?>("StockScanner:Yahoo:RequestTimeoutSeconds") ?? 12;

	client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
	client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 3, 30));
	client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 StockInvestmentScanner/1.0");
});

var app = builder.Build();

app.MapMcp("/mcp");

app.MapGet("/", async (HttpContext httpContext, CancellationToken cancellationToken) =>
{
	var stockScannerTools = httpContext.RequestServices.GetRequiredService<StockScannerTools>();
	var monthlyBudget = 200m;
	const int topCount = 10;

	var plan = await stockScannerTools.CalculateDynamicOpportunitiesAsync(
		monthlyBudget: monthlyBudget,
		topCount: topCount,
		cancellationToken: cancellationToken);

	var html = BuildMainPageHtml(plan);
	return Results.Content(html, "text/html");
});

app.MapGet("/simulate", (HttpContext httpContext) =>
{
	var symbolsQuery = httpContext.Request.Query["symbols"].ToString();
	var symbols = symbolsQuery
		.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
		.Where(s => !string.IsNullOrWhiteSpace(s))
		.Distinct(StringComparer.OrdinalIgnoreCase)
		.Take(10)
		.ToArray();

	if (symbols.Length == 0)
	{
		symbols = ["MSFT", "AAPL", "GOOGL", "AMZN", "NVDA", "BRK.B", "V", "COST", "JNJ", "PG"];
	}

	var html = BuildSimulationPageHtml(symbols);
	return Results.Content(html, "text/html");
});

app.MapGet("/api/plan", async (HttpContext httpContext, CancellationToken cancellationToken) =>
{
	var stockScannerTools = httpContext.RequestServices.GetRequiredService<StockScannerTools>();
	var query = httpContext.Request.Query;
	const decimal defaultMonthlyBudget = 200m;

	var topCount = int.TryParse(query["topCount"], out var parsedTopCount)
		? parsedTopCount
		: 10;
	var strategy = query["strategy"].ToString();
	var useAggressive = string.IsNullOrWhiteSpace(strategy) || strategy.Equals("aggressive", StringComparison.OrdinalIgnoreCase);

	var plan = useAggressive
		? await stockScannerTools.CalculateDynamicOpportunitiesAsync(
			monthlyBudget: defaultMonthlyBudget,
			topCount: topCount,
			cancellationToken: cancellationToken)
		: await stockScannerTools.CalculateTopStocksPlanAsync(
			monthlyBudget: defaultMonthlyBudget,
			topCount: topCount,
			cancellationToken: cancellationToken);

	return Results.Ok(new
	{
		Service = "StockInvestment",
		McpEndpoint = "/mcp",
		ToolsUsed = useAggressive
			? new[] { "calculate_dynamic_opportunities" }
			: new[] { "scan_stocks", "calculate_top_stocks_plan" },
		Input = new
		{
			TopCount = topCount,
			Strategy = useAggressive ? "aggressive" : "reliable"
		},
		Plan = new
		{
			plan.StockCount,
			plan.LiveDataAvailable,
			plan.TopStocks,
			plan.Allocation,
			plan.Guidance
		}
	});
});

app.Run();

static string BuildMainPageHtml(TopStocksInvestmentPlan plan)
{
	var rows = string.Join(Environment.NewLine, plan.TopStocks.Select((stock, index) =>
	{
		var allocation = plan.Allocation.FirstOrDefault(a => a.Symbol.Equals(stock.Symbol, StringComparison.OrdinalIgnoreCase));
		var priceText = stock.LastPrice.HasValue ? stock.LastPrice.Value.ToString("0.00", CultureInfo.InvariantCulture) : "N/A";
		var dayText = stock.DailyChangePercent.HasValue ? $"{stock.DailyChangePercent.Value:0.00}%" : "N/A";
		var monthlyText = allocation is not null ? allocation.MonthlyAmount.ToString("0.00", CultureInfo.InvariantCulture) : "0.00";

		return $"""
		<tr>
			<td>{index + 1}</td>
			<td><strong>{stock.Symbol}</strong></td>
			<td>{stock.ReliabilityScore}</td>
			<td>${priceText}</td>
			<td>{dayText}</td>
			<td>${monthlyText}</td>
			<td>{stock.Reason}</td>
		</tr>
		""";
	}));

	var symbolQuery = Uri.EscapeDataString(string.Join(',', plan.TopStocks.Select(s => s.Symbol)));
	var dataStatus = plan.LiveDataAvailable
		? "Live market quotes loaded"
		: "Live quotes unavailable, ranking uses fallback risk model";

	return $$"""
<!doctype html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title>Investement calculations</title>
	<link href="https://fonts.googleapis.com/icon?family=Material+Icons" rel="stylesheet" />
	<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/materialize/1.0.0/css/materialize.min.css" />
	<style>
		body {
			background: linear-gradient(135deg, #f3f9ff 0%, #fff8f1 100%);
			min-height: 100vh;
		}
		.hero {
			padding: 48px 0 18px;
		}
		.hero h3 {
			font-weight: 700;
			letter-spacing: 0.3px;
		}
		.card-panel.soft {
			border-radius: 14px;
		}
		table.striped > tbody > tr:nth-child(odd) {
			background-color: rgba(0, 121, 107, 0.06);
		}
	</style>
</head>
<body>
	<div class="container hero">
		<div class="row valign-wrapper">
			<div class="col s12 m8">
				<h3 class="teal-text text-darken-2">Investement calculations</h3>
				<p class="grey-text text-darken-1">Top 10 aggressive opportunities auto-evaluated on startup by the MCP server with live market data.</p>
			</div>
			<div class="col s12 m4 right-align">
				<a href="/simulate?symbols={{symbolQuery}}" class="waves-effect waves-light btn-large teal darken-2">
					<i class="material-icons left">timeline</i>
					Simulate Investments
				</a>
			</div>
		</div>

		<div class="card-panel soft">
			<div class="row" style="margin-bottom: 0;">
				<div class="col s12 m6"><strong>Stocks:</strong> {{plan.StockCount}}</div>
				<div class="col s12 m6"><strong>Status:</strong> {{dataStatus}}</div>
				<div class="col s12" style="margin-top: 8px;">
					<strong>Data sources:</strong>
					<ul class="browser-default" style="margin-top: 6px; margin-bottom: 0;">
						<li>Yahoo Finance Spark API (1M price series)</li>
						<li>MCP tool <code>calculate_dynamic_opportunities</code></li>
						<li>Internal fallback risk scoring when live quotes are unavailable</li>
					</ul>
				</div>
			</div>
		</div>

		<div class="card-panel soft">
			<h5 class="teal-text text-darken-3" style="margin-top: 0;">Risky business</h5>
			<table class="striped responsive-table">
				<thead>
					<tr>
						<th>#</th>
						<th>Symbol</th>
						<th>Opportunity</th>
						<th>Last Price</th>
						<th>1M Move</th>
						<th>Monthly Suggestion</th>
						<th>Risk Thesis</th>
					</tr>
				</thead>
				<tbody>
{{rows}}
				</tbody>
			</table>
		</div>

		<div class="row" style="margin-top: 8px;">
			<div class="col s12 center-align">
				<button type="button" id="update-stocks-btn" class="waves-effect waves-light btn amber darken-2">
					<i class="material-icons left">refresh</i>
					Update Stocks
				</button>
			</div>
		</div>
	</div>

	<script src="https://cdnjs.cloudflare.com/ajax/libs/materialize/1.0.0/js/materialize.min.js"></script>
	<script>
		document.getElementById('update-stocks-btn').addEventListener('click', function () {
			window.location.href = '/?refresh=' + Date.now();
		});
	</script>
</body>
</html>
""";
}

static string BuildSimulationPageHtml(string[] symbols)
{
	var rows = string.Join(Environment.NewLine, symbols.Select((symbol, index) =>
		$$"""
		<tr>
			<td><strong>{{symbol}}</strong></td>
			<td>
				<div class="input-field" style="margin: 0;">
					<input id="amount-{{index}}" type="number" min="0" step="1" value="50" class="monthly-input" data-symbol="{{symbol}}" />
				</div>
			</td>
		</tr>
		"""));

	return $$"""
<!doctype html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title>Simulate Investments</title>
	<link href="https://fonts.googleapis.com/icon?family=Material+Icons" rel="stylesheet" />
	<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/materialize/1.0.0/css/materialize.min.css" />
	<style>
		body {
			background: linear-gradient(135deg, #fffef7 0%, #edf7f5 100%);
			min-height: 100vh;
		}
		.section-wrap {
			padding-top: 40px;
		}
		.card-panel {
			border-radius: 14px;
		}
		.metric {
			font-size: 1.45rem;
			font-weight: 700;
		}
	</style>
</head>
<body>
	<div class="container section-wrap">
		<div class="row valign-wrapper">
			<div class="col s12 m8">
				<h4 class="teal-text text-darken-2">Simulate Investments</h4>
				<p class="grey-text text-darken-1">Enter monthly amount per stock and choose your horizon (1-20 years).</p>
			</div>
			<div class="col s12 m4 right-align">
				<a href="/" class="waves-effect waves-light btn-flat teal-text text-darken-2">Back to startup page</a>
			</div>
		</div>

		<div class="row">
			<div class="col s12 m4">
				<div class="input-field">
					<select id="years-select">
						<option value="1">1 year</option>
						<option value="2">2 years</option>
						<option value="3">3 years</option>
						<option value="4">4 years</option>
						<option value="5">5 years</option>
						<option value="6">6 years</option>
						<option value="7">7 years</option>
						<option value="8">8 years</option>
						<option value="9">9 years</option>
						<option value="10" selected>10 years</option>
						<option value="11">11 years</option>
						<option value="12">12 years</option>
						<option value="13">13 years</option>
						<option value="14">14 years</option>
						<option value="15">15 years</option>
						<option value="16">16 years</option>
						<option value="17">17 years</option>
						<option value="18">18 years</option>
						<option value="19">19 years</option>
						<option value="20">20 years</option>
					</select>
					<label>Investment period</label>
				</div>
			</div>
			<div class="col s12 m4">
				<div class="input-field">
					<input id="annual-return" type="number" min="0" max="30" step="0.1" value="10" />
					<label for="annual-return" class="active">Expected annual return (%)</label>
				</div>
			</div>
			<div class="col s12 m4" style="padding-top: 18px;">
				<button id="calculate-btn" class="waves-effect waves-light btn teal darken-2">
					<i class="material-icons left">calculate</i>
					Calculate
				</button>
			</div>
		</div>

		<div class="card-panel">
			<table class="striped responsive-table">
				<thead>
					<tr>
						<th>Stock</th>
						<th>Monthly Investment ($)</th>
					</tr>
				</thead>
				<tbody>
{{rows}}
				</tbody>
			</table>
		</div>

		<div class="row">
			<div class="col s12 m4"><div class="card-panel"><div>Total monthly</div><div id="total-monthly" class="metric">$0.00</div></div></div>
			<div class="col s12 m4"><div class="card-panel"><div>Total invested</div><div id="total-invested" class="metric">$0.00</div></div></div>
			<div class="col s12 m4"><div class="card-panel"><div>Projected value</div><div id="total-value" class="metric">$0.00</div></div></div>
		</div>

		<div class="card-panel">
			<table class="striped responsive-table" id="results-table">
				<thead>
					<tr>
						<th>Stock</th>
						<th>Monthly ($)</th>
						<th>Invested ($)</th>
						<th>Projected Value ($)</th>
						<th>Estimated Profit ($)</th>
					</tr>
				</thead>
				<tbody></tbody>
			</table>
		</div>
	</div>

	<script src="https://cdnjs.cloudflare.com/ajax/libs/materialize/1.0.0/js/materialize.min.js"></script>
	<script>
		document.addEventListener('DOMContentLoaded', function () {
			M.FormSelect.init(document.querySelectorAll('select'));
			const calculateBtn = document.getElementById('calculate-btn');
			calculateBtn.addEventListener('click', runCalculation);
			runCalculation();
		});

		function formatMoney(value) {
			return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', minimumFractionDigits: 2 }).format(value);
		}

		function futureValue(monthly, annualRatePercent, years) {
			const r = annualRatePercent / 100;
			if (r === 0) {
				return monthly * 12 * years;
			}

			const monthlyRate = r / 12;
			const periods = years * 12;
			return monthly * ((Math.pow(1 + monthlyRate, periods) - 1) / monthlyRate);
		}

		function runCalculation() {
			const years = parseInt(document.getElementById('years-select').value, 10);
			const annualReturn = parseFloat(document.getElementById('annual-return').value || '0');
			const rows = Array.from(document.querySelectorAll('.monthly-input'));

			let totalMonthly = 0;
			let totalInvested = 0;
			let totalProjected = 0;
			let resultRows = '';

			for (const input of rows) {
				const symbol = input.dataset.symbol;
				const monthly = Math.max(0, parseFloat(input.value || '0'));
				const invested = monthly * 12 * years;
				const projected = futureValue(monthly, annualReturn, years);
				const profit = projected - invested;

				totalMonthly += monthly;
				totalInvested += invested;
				totalProjected += projected;

				resultRows += `
					<tr>
						<td><strong>${symbol}</strong></td>
						<td>${monthly.toFixed(2)}</td>
						<td>${invested.toFixed(2)}</td>
						<td>${projected.toFixed(2)}</td>
						<td>${profit.toFixed(2)}</td>
					</tr>`;
			}

			document.querySelector('#results-table tbody').innerHTML = resultRows;
			document.getElementById('total-monthly').textContent = formatMoney(totalMonthly);
			document.getElementById('total-invested').textContent = formatMoney(totalInvested);
			document.getElementById('total-value').textContent = formatMoney(totalProjected);
		}
	</script>
</body>
</html>
""";
}
