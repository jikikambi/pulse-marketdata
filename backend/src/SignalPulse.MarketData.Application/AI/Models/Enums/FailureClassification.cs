namespace SignalPulse.MarketData.Application.AI.Models.Enums;

public enum FailureClassification
{
    Unknown,
    Timeout,
    DependencyUnavailable,
    DataCorruption,
    InfrastructureFailure
}