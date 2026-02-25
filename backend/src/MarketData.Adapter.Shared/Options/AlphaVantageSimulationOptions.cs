namespace MarketData.Adapter.Shared.Options;

public sealed class AlphaVantageSimulationOptions
{
    public bool EnableSimulation { get; set; } = true;
    public decimal Volatility { get; set; } = 0.005m; // ±0.5% per tick
    public decimal MaxVolumeJitterPercent { get; set; } = 0.10m; // ±10%
}
