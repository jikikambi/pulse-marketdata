//using Polly.Timeout;
//using SignalPulse.MarketData.Application.AI.Models;
//using SignalPulse.MarketData.Application.AI.Models.Enums;
//using System.ComponentModel.DataAnnotations;

//namespace SignalPulse.MarketData.Application.AI.Services.Agents;

//public sealed class FailureClassifier : IFailureClassifier
//{
//    public FailureClassification Classify(IMarketAgentStage stage, Exception ex) => ex switch
//    {
//        TimeoutRejectedException => new(FailureCategory.Timeout, true, RecoveryStrategy.Retry, "LLM request timed out"),
//        HttpRequestException => new(FailureCategory.DependencyUnavailable, true, RecoveryStrategy.Degrade, "dependency_failure"),
//        TaskCanceledException => new(FailureCategory.Timeout, false, RecoveryStrategy.Retry, "operation_cancelled"),
//        ValidationException => new(FailureCategory.ValidationFailure, false, RecoveryStrategy.Fallback, "validation_failure"),
//        UnauthorizedAccessException => new(FailureCategory.SecurityViolation, false, RecoveryStrategy.Terminate, "security_violation"),
//        InvalidOperationException => new(FailureCategory.ConfigurationError, false, RecoveryStrategy.Terminate, "configuration_error"),
//        _ => new(FailureCategory.Unknown, false, RecoveryStrategy.Terminate, "unknown_failure")
//    };
//}