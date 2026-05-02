using FluentValidation;
using MediatR;
using SharedKernel.Domain;
namespace SharedKernel.Behaviours;
/// <summary>
/// MediatR pipeline behaviour that runs all registered FluentValidation validators
/// for a command before the handler executes. If any rules fail, returns a
/// Result.Failure with all error messages — the handler never runs.
///
/// Works for commands that return Result&lt;T&gt;. For void Result commands,
/// see ValidationBehaviourVoid below.
/// </summary>
public class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var failures = validators
            .Select(v => v.Validate(request))
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .ToList();

        if (failures.Count == 0) return await next();

        // If TResponse is Result<T>, return Failure without throwing
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var errors = failures.Select(f => f.ErrorMessage);
            var failureMethod = responseType.GetMethod("Failure", [typeof(IEnumerable<string>)])!;
            return (TResponse)failureMethod.Invoke(null, [errors])!;
        }

        // For non-Result responses, throw so GlobalExceptionMiddleware catches it
        throw new ValidationException(failures);
    }
}
