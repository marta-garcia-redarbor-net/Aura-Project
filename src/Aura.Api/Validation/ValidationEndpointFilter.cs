using FluentValidation;

namespace Aura.Api.Validation;

/// <summary>
/// Endpoint filter that uses an <see cref="IValidator{T}"/> from DI to validate
/// the request DTO. Returns HTTP 422 with RFC 4918 error shape on failure.
/// </summary>
public sealed class ValidationEndpointFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationEndpointFilter(IValidator<T> validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return await next(context);
        }

        var validationResult = await _validator.ValidateAsync(argument);
        if (validationResult.IsValid)
        {
            return await next(context);
        }

        var errors = validationResult.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors, statusCode: StatusCodes.Status422UnprocessableEntity);
    }
}
