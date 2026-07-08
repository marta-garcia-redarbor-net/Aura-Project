namespace Aura.ArchitectureTests;

public class AcaBicepFoundationTests
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Theory]
    [InlineData("infra/aca/main.bicep")]
    [InlineData("infra/aca/api.bicep")]
    [InlineData("infra/aca/ui.bicep")]
    [InlineData("infra/aca/workers.bicep")]
    [InlineData("infra/aca/sql-database.bicep")]
    public void FoundationBicepFiles_Exist(string relativePath)
    {
        var absolutePath = Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(absolutePath), $"Expected Bicep file at '{absolutePath}'.");
    }

    [Fact]
    public void MainBicep_WiresResourceGroupManagedEnvironmentAndModules()
    {
        var content = ReadBicep("infra/aca/main.bicep");

        Assert.Contains("targetScope = 'subscription'", content, StringComparison.Ordinal);
        Assert.Contains("resource rg 'Microsoft.Resources/resourceGroups@", content, StringComparison.Ordinal);
        Assert.Contains("resource managedEnvironment 'Microsoft.App/managedEnvironments@", content, StringComparison.Ordinal);
        Assert.Contains("module sqlDatabase './sql-database.bicep'", content, StringComparison.Ordinal);
        Assert.Contains("module api './api.bicep'", content, StringComparison.Ordinal);
        Assert.Contains("module ui './ui.bicep'", content, StringComparison.Ordinal);
        Assert.Contains("module workers './workers.bicep'", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiBicep_DefinesIngressCorsAndContainerEnvVariables()
    {
        var content = ReadBicep("infra/aca/api.bicep");

        Assert.Contains("resource containerApp 'Microsoft.App/containerApps@", content, StringComparison.Ordinal);
        Assert.Contains("ingress:", content, StringComparison.Ordinal);
        Assert.Contains("external: true", content, StringComparison.Ordinal);
        Assert.Contains("corsPolicy", content, StringComparison.Ordinal);
        Assert.Contains("allowedOrigins", content, StringComparison.Ordinal);
        Assert.Contains("env:", content, StringComparison.Ordinal);
    }

    [Fact]
    public void UiBicep_DefinesPublicIngressAndContainerEnvVariables()
    {
        var content = ReadBicep("infra/aca/ui.bicep");

        Assert.Contains("resource containerApp 'Microsoft.App/containerApps@", content, StringComparison.Ordinal);
        Assert.Contains("ingress:", content, StringComparison.Ordinal);
        Assert.Contains("external: true", content, StringComparison.Ordinal);
        Assert.Contains("env:", content, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkersBicep_DefinesContainerAppWithoutIngress()
    {
        var content = ReadBicep("infra/aca/workers.bicep");

        Assert.Contains("resource containerApp 'Microsoft.App/containerApps@", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ingress:", content, StringComparison.Ordinal);
        Assert.Contains("env:", content, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlDatabaseBicep_DefinesServerDatabaseAndAllowAzureServicesFirewallRule()
    {
        var content = ReadBicep("infra/aca/sql-database.bicep");

        Assert.Contains("resource sqlServer 'Microsoft.Sql/servers@", content, StringComparison.Ordinal);
        Assert.Contains("resource database 'Microsoft.Sql/servers/databases@", content, StringComparison.Ordinal);
        Assert.Contains("resource allowAzureServicesRule 'Microsoft.Sql/servers/firewallRules@", content, StringComparison.Ordinal);
        Assert.Contains("startIpAddress: '0.0.0.0'", content, StringComparison.Ordinal);
        Assert.Contains("endIpAddress: '0.0.0.0'", content, StringComparison.Ordinal);
    }

    private static string ReadBicep(string relativePath)
    {
        var absolutePath = Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(absolutePath);
    }
}
