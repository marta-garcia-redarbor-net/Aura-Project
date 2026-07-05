using Aura.Application.Ports;
using Aura.Domain.FocusState;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aura.Api.Endpoints;

public static class FocusStateEndpoints
{
    public static IEndpointRouteBuilder MapFocusStateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/focus-state")
            .RequireAuthorization();

        group.MapGet("/current", GetCurrentAsync);

        return endpoints;
    }

    private static async Task<IResult> GetCurrentAsync(
        ICurrentUserService currentUserService,
        IFocusStateResolver focusStateResolver,
        CancellationToken cancellationToken)
    {
        var currentUser = currentUserService.GetCurrentUser();
        var userOid = currentUser?.Oid;

        if (string.IsNullOrWhiteSpace(userOid))
        {
            return Results.Unauthorized();
        }

        var state = await focusStateResolver.ResolveAsync(userOid, cancellationToken);

        return Results.Ok(new FocusStateResponse(
            CurrentState: state.CurrentState.ToString(),
            Label: null,
            Since: DateTimeOffset.UtcNow,
            Signals: [$"resolved:{state.CurrentState}"]));
    }

    public sealed record FocusStateResponse(
        string CurrentState,
        string? Label,
        DateTimeOffset Since,
        IReadOnlyList<string> Signals);
}
