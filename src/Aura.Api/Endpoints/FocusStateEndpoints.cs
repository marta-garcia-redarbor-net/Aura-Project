using System.Diagnostics;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Api.Validation;
using Aura.Domain.FocusState;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Endpoints;

/// <summary>
/// Focus state management endpoints for viewing and overriding the current focus state.
/// </summary>
public static partial class FocusStateEndpoints
{
    private static readonly ActivitySource ActivitySource = new("Aura.Api");

    /// <summary>
    /// Maps focus state routes under <c>/api/focus-state</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapFocusStateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/focus-state")
            .RequireAuthorization("RequireEntraId");

        group.MapGet("/", GetFocusStateAsync);
        group.MapPut("/", SetFocusStateAsync)
            .AddEndpointFilter<ValidationEndpointFilter<SetFocusStateRequest>>();
        group.MapDelete("/", ClearFocusStateAsync);

        return endpoints;
    }

    private static async Task<IResult> GetFocusStateAsync(
        ICurrentUserService currentUserService,
        IFocusStateResolver focusStateResolver,
        IFocusStateOverrideStore overrideStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.FocusState");

        using var activity = ActivitySource.StartActivity("focus-state.get", ActivityKind.Server);
        activity?.SetTag("focus-state.endpoint", "GET /api/focus-state");

        try
        {
            var user = currentUserService.GetCurrentUser();
            var userId = user?.UserId ?? "anonymous";
            activity?.SetTag("focus-state.user_id", userId);

            var focusState = await focusStateResolver.ResolveAsync(userId, cancellationToken);
            var activeOverride = await overrideStore.GetAsync(userId, cancellationToken);

            var response = new FocusStateResponse
            {
                State = focusState.CurrentState.ToString(),
                IsOverridden = activeOverride.HasValue,
                UserId = userId
            };

            activity?.SetTag("focus-state.state", response.State);
            activity?.SetTag("focus-state.is_override", response.IsOverridden);

            Log.FocusStateSucceeded(logger, response.State, response.IsOverridden);

            return Results.Ok(response);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.FocusStateCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.FocusStateFailed(logger, ex);
            return Results.Problem(
                title: "Focus state request failed",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> SetFocusStateAsync(
        ICurrentUserService currentUserService,
        IFocusStateOverrideStore overrideStore,
        ILoggerFactory loggerFactory,
        SetFocusStateRequest? request,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.FocusState");

        using var activity = ActivitySource.StartActivity("focus-state.set", ActivityKind.Server);
        activity?.SetTag("focus-state.endpoint", "PUT /api/focus-state");

        try
        {
            var user = currentUserService.GetCurrentUser();
            var userId = user?.UserId ?? "anonymous";
            activity?.SetTag("focus-state.user_id", userId);

            if (request is null || request.State is null)
            {
                await overrideStore.ClearAsync(userId);
                activity?.SetTag("focus-state.cleared", true);
                Log.FocusStateOverrideCleared(logger);
            }
            else
            {
                if (!Enum.TryParse<FocusStateType>(request.State, ignoreCase: true, out var parsedState))
                {
                    return Results.Problem(
                        title: "Invalid focus state",
                        detail: $"State '{request.State}' is not valid. Valid values: {string.Join(", ", Enum.GetNames<FocusStateType>())}",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                await overrideStore.SetAsync(userId, parsedState);
                activity?.SetTag("focus-state.set_state", parsedState.ToString());
                Log.FocusStateOverrideSet(logger, parsedState.ToString());
            }

            return Results.Ok();
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.FocusStateCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.FocusStateFailed(logger, ex);
            return Results.Problem(
                title: "Focus state request failed",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ClearFocusStateAsync(
        ICurrentUserService currentUserService,
        IFocusStateOverrideStore overrideStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.FocusState");

        using var activity = ActivitySource.StartActivity("focus-state.clear", ActivityKind.Server);
        activity?.SetTag("focus-state.endpoint", "DELETE /api/focus-state");

        try
        {
            var user = currentUserService.GetCurrentUser();
            var userId = user?.UserId ?? "anonymous";
            activity?.SetTag("focus-state.user_id", userId);

            await overrideStore.ClearAsync(userId);

            Log.FocusStateOverrideCleared(logger);

            return Results.Ok();
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.FocusStateCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.FocusStateFailed(logger, ex);
            return Results.Problem(
                title: "Focus state request failed",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3001, Level = LogLevel.Information,
            Message = "Focus state resolved: {State} (override={IsOverride})")]
        public static partial void FocusStateSucceeded(ILogger logger, string state, bool isOverride);

        [LoggerMessage(EventId = 3002, Level = LogLevel.Warning,
            Message = "Focus state request was cancelled")]
        public static partial void FocusStateCancelled(ILogger logger);

        [LoggerMessage(EventId = 3003, Level = LogLevel.Error,
            Message = "Focus state request failed")]
        public static partial void FocusStateFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 3004, Level = LogLevel.Information,
            Message = "Focus state override set to {State}")]
        public static partial void FocusStateOverrideSet(ILogger logger, string state);

        [LoggerMessage(EventId = 3005, Level = LogLevel.Information,
            Message = "Focus state override cleared")]
        public static partial void FocusStateOverrideCleared(ILogger logger);
    }
}

/// <summary>
/// Request model for setting a focus state override via PUT /api/focus-state.
/// </summary>
public sealed record SetFocusStateRequest(string? State);
