namespace Aura.ArchitectureTests;

using NetArchTest.Rules;

public class SemanticIndexArchitectureTests
{
    private const string DomainNamespace = "Aura.Domain";
    private const string ApplicationNamespace = "Aura.Application";
    private const string InfrastructureNamespace = "Aura.Infrastructure";

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        var result = Types
            .InAssembly(typeof(Aura.Domain.SemanticIndex.ValueObjects.SemanticChunk).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references Infrastructure: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Domain_ShouldNotReference_QdrantSdk()
    {
        var result = Types
            .InAssembly(typeof(Aura.Domain.SemanticIndex.ValueObjects.SemanticChunk).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Qdrant.Client")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references Qdrant SDK: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Application_ShouldNotReference_Infrastructure()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.ISemanticIndexWriter).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references Infrastructure: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Application_ShouldNotReference_QdrantSdk()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.ISemanticIndexWriter).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Qdrant.Client")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references Qdrant SDK: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Domain_SemanticTypes_MustResideInSemanticIndexSubNamespaces()
    {
        var domainAssembly = typeof(Aura.Domain.SemanticIndex.ValueObjects.SemanticChunk).Assembly;

        var semanticTypes = domainAssembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested)
            .Where(t => t.Name.Contains("Semantic") || t.Name == "DomainTag")
            .ToList();

        Assert.NotEmpty(semanticTypes);

        var allowedNamespaces = new[]
        {
            "Aura.Domain.SemanticIndex.Enums",
            "Aura.Domain.SemanticIndex.ValueObjects"
        };

        var misplaced = semanticTypes
            .Where(t => !allowedNamespaces.Contains(t.Namespace))
            .Select(t => $"{t.FullName} (in {t.Namespace})")
            .ToList();

        Assert.True(misplaced.Count == 0,
            $"Semantic domain types outside allowed sub-namespaces: {string.Join(", ", misplaced)}");
    }

    [Fact]
    public void Application_Ports_ShouldBeInterfaces()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.ISemanticIndexWriter).Assembly)
            .That()
            .ResideInNamespace("Aura.Application.Ports")
            .And()
            .HaveNameStartingWith("I")
            .Should()
            .BeInterfaces()
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application Ports with 'I' prefix should be interfaces: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";
        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
