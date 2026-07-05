using Aura.Application.Ports;
using Aura.Domain.FocusState;

namespace Aura.UnitTests.Application;

public class FocusStateOverrideStoreContractTests
{
    [Fact]
    public void Interface_ExposesGetAsync_WithExpectedSignature()
    {
        var methods = typeof(IFocusStateOverrideStore).GetMethods();

        var getMethod = methods.FirstOrDefault(m =>
            m.Name == nameof(IFocusStateOverrideStore.GetAsync));

        Assert.NotNull(getMethod);
        Assert.Equal(typeof(Task<FocusStateType?>), getMethod.ReturnType);

        var parameters = getMethod.GetParameters();
        Assert.Contains(parameters, p => p.Name == "userId" && p.ParameterType == typeof(string));
        Assert.Contains(parameters, p => p.Name == "cancellationToken" && p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public void Interface_ExposesSetAsync_WithExpectedSignature()
    {
        var methods = typeof(IFocusStateOverrideStore).GetMethods();

        var setMethod = methods.FirstOrDefault(m =>
            m.Name == nameof(IFocusStateOverrideStore.SetAsync));

        Assert.NotNull(setMethod);
        Assert.Equal(typeof(Task), setMethod.ReturnType);

        var parameters = setMethod.GetParameters();
        Assert.Contains(parameters, p => p.Name == "userId" && p.ParameterType == typeof(string));
        Assert.Contains(parameters, p => p.Name == "state" && p.ParameterType == typeof(FocusStateType));
    }

    [Fact]
    public void Interface_ExposesClearAsync_WithExpectedSignature()
    {
        var methods = typeof(IFocusStateOverrideStore).GetMethods();

        var clearMethod = methods.FirstOrDefault(m =>
            m.Name == nameof(IFocusStateOverrideStore.ClearAsync));

        Assert.NotNull(clearMethod);
        Assert.Equal(typeof(Task), clearMethod.ReturnType);

        var parameters = clearMethod.GetParameters();
        Assert.Contains(parameters, p => p.Name == "userId" && p.ParameterType == typeof(string));
    }

    [Fact]
    public void Interface_HasNoInfrastructureDependency()
    {
        var referencedAssemblies = typeof(IFocusStateOverrideStore)
            .Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet();

        Assert.DoesNotContain("Aura.Infrastructure", referencedAssemblies);
    }
}
