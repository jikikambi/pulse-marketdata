using SignalPulse.Common.Errors;

namespace SignalPulse.MarketData.Domain.Exceptions;

public class InvalidQuoteException(string message) : DomainException(message)
{}