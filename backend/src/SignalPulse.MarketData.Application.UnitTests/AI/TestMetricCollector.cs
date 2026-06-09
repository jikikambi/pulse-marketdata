using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace SignalPulse.MarketData.Application.UnitTests.AI;

public sealed class TestMetricCollector : IDisposable
{
    private readonly MeterListener _listener;

    public ConcurrentDictionary<string, long> Counters { get; } = [];
    public ConcurrentDictionary<string, List<double>> Histograms { get; } = [];

    public TestMetricCollector()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "SignalPulse.MarketAgent")
                    listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            Counters.AddOrUpdate(instrument.Name, value, (_, existing) => existing + value);
        });

        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
        {
            Histograms.AddOrUpdate(instrument.Name, [value], (_, list) =>
            {
                list.Add(value);
                return list;
            });
        });

        _listener.Start();
    }

    public void Dispose() => _listener.Dispose();
}