namespace SignalPulse.MarketData.Application.AI.Models.Enums;

public enum FailureCategory
{
    Unknown,
    Transient,
    DependencyUnavailable,
    Timeout,
    DataCorruption,
    ValidationFailure,
    SecurityViolation,
    ConfigurationError,
    BusinessRuleViolation
}