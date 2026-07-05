using System.Reflection;

namespace Aura.UnitTests.Application;

public class InterruptionDecisionStoreContractTests
{
    private static readonly Type StoreInterface = typeof(Aura.Application.Ports.IInterruptionDecisionStore);

    [Fact]
    public void Interface_ExposesRecordAsync_WithExpectedSignature()
    {
        var method = StoreInterface.GetMethod("RecordAsync");

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);

        var parameters = method.GetParameters();
        Assert.Contains(parameters, p => p.Name == "record"
            && p.ParameterType.Name == "InterruptionDecisionRecord"
            && p.ParameterType.Namespace == "Aura.Application.Models");
        Assert.Contains(parameters, p => p.Name == "cancellationToken"
            && p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public void Interface_ExposesQueryAsync_WithExpectedSignature()
    {
        var method = StoreInterface.GetMethod("QueryAsync");

        Assert.NotNull(method);
        Assert.True(method!.ReturnType.IsGenericType);
        Assert.Equal("PagedResult`1", method.ReturnType.GenericTypeArguments[0].Name);
        Assert.Equal("InterruptionDecisionRecord", method.ReturnType.GenericTypeArguments[0].GenericTypeArguments[0].Name);

        var parameters = method.GetParameters();
        Assert.Contains(parameters, p => p.Name == "page" && p.ParameterType == typeof(int));
        Assert.Contains(parameters, p => p.Name == "pageSize" && p.ParameterType == typeof(int));
        Assert.Contains(parameters, p => p.Name == "cancellationToken"
            && p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public void Interface_HasNoInfrastructureDependency()
    {
        var referencedAssemblies = StoreInterface
            .Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet();

        Assert.DoesNotContain("Aura.Infrastructure", referencedAssemblies);
    }
}
