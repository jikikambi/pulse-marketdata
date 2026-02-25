namespace SignalPulse.Abstractions.Events;

public interface ISignalREvent<out TPayload>
{
    string EventType { get; }
    TPayload Payload { get; }
}

