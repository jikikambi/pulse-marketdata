using SignalPulse.MarketData.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<MarketDataWorker>();

var host = builder.Build();
host.Run();
