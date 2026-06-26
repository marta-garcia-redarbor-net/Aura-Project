using System.Xml.Linq;

namespace Aura.ArchitectureTests;

public class ProjectFileArchitectureTests
{
    [Fact]
    public void AuraE2E_HasExactlyOnePlaywrightPackageReference()
    {
        var projectPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "tests", "Aura.E2E", "Aura.E2E.csproj");

        Assert.True(File.Exists(projectPath), $"E2E project file not found at: {projectPath}");

        var doc = XDocument.Load(projectPath);
        var packageReferences = doc.Descendants("PackageReference")
            .Where(pr => string.Equals(
                pr.Attribute("Include")?.Value,
                "Microsoft.Playwright",
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Equal(1, packageReferences.Count);
    }
}
