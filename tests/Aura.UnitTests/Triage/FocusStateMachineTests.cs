using Aura.Domain.FocusState;
using Aura.Application.Ports;
using NSubstitute;

namespace Aura.UnitTests.Triage;

public sealed class FocusStateMachineTests
{
    // ============================================================
    // FocusStateType enum tests
    // ============================================================

    [Fact]
    public void FocusStateType_ContainsExactlyFourValues()
    {
        var values = Enum.GetValues<FocusStateType>();

        Assert.Equal(4, values.Length);
        Assert.Contains(FocusStateType.DeepWork, values);
        Assert.Contains(FocusStateType.WindowOfOpportunity, values);
        Assert.Contains(FocusStateType.Away, values);
        Assert.Contains(FocusStateType.Recovery, values);
    }

    // ============================================================
    // Initial state
    // ============================================================

    [Fact]
    public void FocusState_NewInstance_StartsInWindowOfOpportunity()
    {
        var state = new Aura.Domain.FocusState.FocusState();

        Assert.Equal(FocusStateType.WindowOfOpportunity, state.CurrentState);
    }

    // ============================================================
    // Valid transitions
    // ============================================================

    [Fact]
    public void GoToAway_FromWindowOfOpportunity_ChangesState()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway();
        Assert.Equal(FocusStateType.Away, state.CurrentState);
    }

    [Fact]
    public void GoToRecovery_FromAway_ChangesState()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway(); // WindowOfOpportunity → Away
        state.GoToRecovery();
        Assert.Equal(FocusStateType.Recovery, state.CurrentState);
    }

    [Fact]
    public void TryEnterDeepWork_FromAway_ChangesState()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway(); // WindowOfOpportunity → Away
        state.TryEnterDeepWork();
        Assert.Equal(FocusStateType.DeepWork, state.CurrentState);
    }

    [Fact]
    public void TryEnterDeepWork_FromRecovery_ChangesState()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway();      // WindowOfOpportunity → Away
        state.GoToRecovery();  // Away → Recovery
        state.TryEnterDeepWork();
        Assert.Equal(FocusStateType.DeepWork, state.CurrentState);
    }

    [Fact]
    public void GoToWindowOfOpportunity_FromDeepWork_ChangesState()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway();           // WindowOfOpportunity → Away
        state.TryEnterDeepWork();   // Away → DeepWork
        state.GoToWindowOfOpportunity();
        Assert.Equal(FocusStateType.WindowOfOpportunity, state.CurrentState);
    }

    [Fact]
    public void GoToWindowOfOpportunity_FromRecovery_ChangesState()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway();           // WindowOfOpportunity → Away
        state.GoToRecovery();       // Away → Recovery
        state.GoToWindowOfOpportunity();
        Assert.Equal(FocusStateType.WindowOfOpportunity, state.CurrentState);
    }

    // ============================================================
    // Invalid transitions — DeepWork
    // ============================================================

    [Fact]
    public void GoToAway_FromDeepWork_ThrowsInvalidOperation()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway();           // WindowOfOpportunity → Away
        state.TryEnterDeepWork();   // Away → DeepWork

        var ex = Assert.Throws<InvalidOperationException>(() => state.GoToAway());
        Assert.Contains("DeepWork", ex.Message);
        Assert.Contains("Away", ex.Message);
        Assert.Equal(FocusStateType.DeepWork, state.CurrentState); // state unchanged
    }

    [Fact]
    public void GoToRecovery_FromDeepWork_ThrowsInvalidOperation()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway();           // WindowOfOpportunity → Away
        state.TryEnterDeepWork();   // Away → DeepWork

        var ex = Assert.Throws<InvalidOperationException>(() => state.GoToRecovery());
        Assert.Contains("DeepWork", ex.Message);
        Assert.Contains("Recovery", ex.Message);
        Assert.Equal(FocusStateType.DeepWork, state.CurrentState);
    }

    [Fact]
    public void TryEnterDeepWork_FromDeepWork_ThrowsInvalidOperation()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway();           // WindowOfOpportunity → Away
        state.TryEnterDeepWork();   // Away → DeepWork

        var ex = Assert.Throws<InvalidOperationException>(() => state.TryEnterDeepWork());
        Assert.Contains("DeepWork", ex.Message);
        Assert.Equal(FocusStateType.DeepWork, state.CurrentState);
    }

    // ============================================================
    // Invalid transitions — WindowOfOpportunity
    // ============================================================

    [Fact]
    public void TryEnterDeepWork_FromWindowOfOpportunity_ThrowsInvalidOperation()
    {
        var state = new Aura.Domain.FocusState.FocusState();

        var ex = Assert.Throws<InvalidOperationException>(() => state.TryEnterDeepWork());
        Assert.Contains("WindowOfOpportunity", ex.Message);
        Assert.Contains("DeepWork", ex.Message);
        Assert.Equal(FocusStateType.WindowOfOpportunity, state.CurrentState);
    }

    [Fact]
    public void GoToRecovery_FromWindowOfOpportunity_ThrowsInvalidOperation()
    {
        var state = new Aura.Domain.FocusState.FocusState();

        var ex = Assert.Throws<InvalidOperationException>(() => state.GoToRecovery());
        Assert.Contains("WindowOfOpportunity", ex.Message);
        Assert.Contains("Recovery", ex.Message);
        Assert.Equal(FocusStateType.WindowOfOpportunity, state.CurrentState);
    }

    [Fact]
    public void GoToWindowOfOpportunity_FromWindowOfOpportunity_ThrowsInvalidOperation()
    {
        var state = new Aura.Domain.FocusState.FocusState();

        var ex = Assert.Throws<InvalidOperationException>(() => state.GoToWindowOfOpportunity());
        Assert.Contains("WindowOfOpportunity", ex.Message);
        Assert.Equal(FocusStateType.WindowOfOpportunity, state.CurrentState);
    }

    // ============================================================
    // Invalid transitions — Away
    // ============================================================

    [Fact]
    public void GoToWindowOfOpportunity_FromAway_ThrowsInvalidOperation()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway(); // WindowOfOpportunity → Away

        var ex = Assert.Throws<InvalidOperationException>(() => state.GoToWindowOfOpportunity());
        Assert.Contains("Away", ex.Message);
        Assert.Contains("WindowOfOpportunity", ex.Message);
        Assert.Equal(FocusStateType.Away, state.CurrentState);
    }

    [Fact]
    public void GoToAway_FromAway_ThrowsInvalidOperation()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway(); // WindowOfOpportunity → Away

        var ex = Assert.Throws<InvalidOperationException>(() => state.GoToAway());
        Assert.Contains("Away", ex.Message);
        Assert.Equal(FocusStateType.Away, state.CurrentState);
    }

    // ============================================================
    // Invalid transitions — Recovery
    // ============================================================

    [Fact]
    public void GoToAway_FromRecovery_ThrowsInvalidOperation()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway();           // WindowOfOpportunity → Away
        state.GoToRecovery();       // Away → Recovery

        var ex = Assert.Throws<InvalidOperationException>(() => state.GoToAway());
        Assert.Contains("Recovery", ex.Message);
        Assert.Contains("Away", ex.Message);
        Assert.Equal(FocusStateType.Recovery, state.CurrentState);
    }

    [Fact]
    public void GoToRecovery_FromRecovery_ThrowsInvalidOperation()
    {
        var state = new Aura.Domain.FocusState.FocusState();
        state.GoToAway();           // WindowOfOpportunity → Away
        state.GoToRecovery();       // Away → Recovery

        var ex = Assert.Throws<InvalidOperationException>(() => state.GoToRecovery());
        Assert.Contains("Recovery", ex.Message);
        Assert.Equal(FocusStateType.Recovery, state.CurrentState);
    }

    // ============================================================
    // IFocusStateResolver port tests
    // ============================================================

    [Fact]
    public void IFocusStateResolver_ExposesResolveAsync_WithExpectedSignature()
    {
        var method = typeof(IFocusStateResolver)
            .GetMethods()
            .FirstOrDefault(m => m.Name == nameof(IFocusStateResolver.ResolveAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Aura.Domain.FocusState.FocusState>), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);

        var hasDefault = parameters[1].HasDefaultValue;
        Assert.True(hasDefault);
    }

    [Fact]
    public void IFocusStateResolver_HasNoInfrastructureDependency()
    {
        var referencedAssemblies = typeof(IFocusStateResolver)
            .Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet();

        Assert.DoesNotContain("Aura.Infrastructure", referencedAssemblies);
    }

    // ============================================================
    // FocusStateResolver tests
    // ============================================================

    [Fact]
    public async Task ResolveAsync_AnyUserId_ReturnsWindowOfOpportunity()
    {
        var overrideStore = Substitute.For<IFocusStateOverrideStore>();
        overrideStore.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((FocusStateType?)null);
        IFocusStateResolver resolver = new Aura.Application.Services.FocusStateResolver(overrideStore);

        var result = await resolver.ResolveAsync("user-123", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(FocusStateType.WindowOfOpportunity, result.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_SameInputs_SameState()
    {
        var overrideStore = Substitute.For<IFocusStateOverrideStore>();
        overrideStore.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((FocusStateType?)null);
        IFocusStateResolver resolver = new Aura.Application.Services.FocusStateResolver(overrideStore);

        var first = await resolver.ResolveAsync("user-123", CancellationToken.None);
        var second = await resolver.ResolveAsync("user-123", CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.CurrentState, second.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_WithPersistedOverride_ReturnsOverrideBeforeAutoComputedState()
    {
        var overrideStore = Substitute.For<IFocusStateOverrideStore>();
        overrideStore.GetAsync("user-123", Arg.Any<CancellationToken>())
            .Returns((FocusStateType?)FocusStateType.DeepWork);
        IFocusStateResolver resolver = new Aura.Application.Services.FocusStateResolver(overrideStore);

        var result = await resolver.ResolveAsync("user-123", CancellationToken.None);

        Assert.Equal(FocusStateType.DeepWork, result.CurrentState);
        await overrideStore.Received(1).GetAsync("user-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAsync_WhenOverrideCleared_FallsBackToAutoComputedState()
    {
        var overrideStore = Substitute.For<IFocusStateOverrideStore>();
        overrideStore.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((FocusStateType?)null);
        IFocusStateResolver resolver = new Aura.Application.Services.FocusStateResolver(overrideStore);

        var result = await resolver.ResolveAsync("user-123", CancellationToken.None);

        Assert.Equal(FocusStateType.WindowOfOpportunity, result.CurrentState);
    }

    // ============================================================
    // Edge cases: empty user ID
    // ============================================================

    [Fact]
    public async Task ResolveAsync_EmptyUserId_DoesNotThrow()
    {
        var overrideStore = Substitute.For<IFocusStateOverrideStore>();
        overrideStore.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((FocusStateType?)null);
        IFocusStateResolver resolver = new Aura.Application.Services.FocusStateResolver(overrideStore);

        var result = await resolver.ResolveAsync("", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(FocusStateType.WindowOfOpportunity, result.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_NullUserId_DoesNotThrow()
    {
        var overrideStore = Substitute.For<IFocusStateOverrideStore>();
        overrideStore.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((FocusStateType?)null);
        IFocusStateResolver resolver = new Aura.Application.Services.FocusStateResolver(overrideStore);

        var result = await resolver.ResolveAsync(null!, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(FocusStateType.WindowOfOpportunity, result.CurrentState);
    }
}
