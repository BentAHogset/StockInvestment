using ModelContextProtocol.AspNetCore;
using StockInvestment.Mcp;
using StockInvestment.Services;

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

builder.Services.AddControllers();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapMcp("/mcp");
app.MapControllers();
app.MapFallbackToFile("index.html"); // let the React SPA handle any non-API route

app.Run();
