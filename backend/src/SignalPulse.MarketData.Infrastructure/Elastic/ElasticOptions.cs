namespace SignalPulse.MarketData.Infrastructure.Elastic;

public sealed class ElasticOptions
{
    public string Url { get; set; } = "http://localhost:9200";
    public string IndexPrefix { get; set; } = "market-agent-events";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool AutoRegisterTemplate { get; set; } = true;
    public string? IngestPipeline { get; set; }
}