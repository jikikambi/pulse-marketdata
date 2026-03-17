using SignalPulse.Common.Errors;

namespace SignalPulse.MarketData.Domain.Exceptions;

public class InvalidForexException(string message) : DomainException(message)
{}
