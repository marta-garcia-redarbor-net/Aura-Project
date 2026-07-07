using Aura.Api.Validation;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.Validation;

public class ValidationEndpointFilterTests
{
    private static readonly EndpointFilterDelegate NextDelegate =
        _ => ValueTask.FromResult<object?>("next-called");

    [Fact]
    public async Task InvokeAsync_WhenValidationPasses_CallsNext()
    {
        var validator = Substitute.For<IValidator<TestDto>>();
        validator.ValidateAsync(Arg.Any<TestDto>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var filter = new ValidationEndpointFilter<TestDto>(validator);
        var context = CreateContext(new TestDto { Name = "valid", Email = "test@example.com" });

        var result = await filter.InvokeAsync(context, NextDelegate);

        Assert.Equal("next-called", result);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationFails_Returns422WithFieldErrors()
    {
        var validator = Substitute.For<IValidator<TestDto>>();
        var failures = new List<ValidationFailure>
        {
            new("Name", "'Name' must not be empty."),
            new("Email", "'Email' must be a valid email address.")
        };
        validator.ValidateAsync(Arg.Any<TestDto>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var filter = new ValidationEndpointFilter<TestDto>(validator);
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        httpContext.RequestServices = services.BuildServiceProvider();
        var context = CreateContext(
            new TestDto { Name = "", Email = "not-an-email" },
            httpContext);

        var result = await filter.InvokeAsync(context, NextDelegate);

        Assert.NotNull(result);
        var httpResult = Assert.IsAssignableFrom<IResult>(result);
        await httpResult.ExecuteAsync(httpContext);
        Assert.Equal(422, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationFails_ErrorBodyContainsFieldNames()
    {
        var validator = Substitute.For<IValidator<TestDto>>();
        var failures = new List<ValidationFailure>
        {
            new("Name", "'Name' must not be empty."),
            new("Email", "'Email' must be a valid email address.")
        };
        validator.ValidateAsync(Arg.Any<TestDto>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var filter = new ValidationEndpointFilter<TestDto>(validator);
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var services2 = new ServiceCollection();
        services2.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        httpContext.RequestServices = services2.BuildServiceProvider();
        var context = CreateContext(
            new TestDto { Name = "", Email = "bad" },
            httpContext);

        var result = await filter.InvokeAsync(context, NextDelegate);

        Assert.NotNull(result);
        var httpResult = Assert.IsAssignableFrom<IResult>(result);
        await httpResult.ExecuteAsync(httpContext);
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();

        Assert.Contains("Name", body, StringComparison.Ordinal);
        Assert.Contains("Email", body, StringComparison.Ordinal);
        Assert.Contains("must not be empty", body, StringComparison.Ordinal);
    }

    private static EndpointFilterInvocationContext CreateContext(
        object? argument, HttpContext? httpContext = null)
    {
        return new TestInvocationContext(httpContext ?? new DefaultHttpContext(), [argument]);
    }

    private sealed class TestInvocationContext(HttpContext httpContext, object?[] arguments)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext => httpContext;
        public override IList<object?> Arguments => arguments;
        public override T GetArgument<T>(int index) => (T)arguments[index]!;
    }

    public sealed class TestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
