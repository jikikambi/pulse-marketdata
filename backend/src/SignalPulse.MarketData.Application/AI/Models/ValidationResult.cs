using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record ValidationResult(bool IsValid, string Reason, ValidationSeverity Severity);
