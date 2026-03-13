using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MarketData.Adapter.Shared.Middleware;

public sealed class RequestCancellationMiddleware(RequestDelegate next,
    ILogger<RequestCancellationMiddleware> logger)
{

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request cancelled by client: {Method} {Path}", context.Request.Method, context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499; // client closed request
            }
        }
    }
}