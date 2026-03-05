using FluentValidation;
namespace MarketData.Adapter.Shared.AlphaVantage.Services;

public class ValidatedApiClient<TRequest, TResponse>(IValidator<TRequest> validator)
{
    public async Task<TResponse> Execute(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> call, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);

        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        return await call(request, ct);
    }
}