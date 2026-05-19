using Microsoft.Extensions.Logging;

namespace SignalPulse.MarketData.Infrastructure.Elastic;

public sealed class LoggerWorkflowEventSink(ILogger<LoggerWorkflowEventSink> logger)
    : IWorkflowEventSink
{
    public Task WriteAsync(WorkflowEvent workflowEvent, CancellationToken ct = default)
    {
        logger.LogInformation("[{Stage}] {EventType} - {Message}", workflowEvent.Stage, workflowEvent.EventType, workflowEvent.Message);

        return Task.CompletedTask;
    }
}