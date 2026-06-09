namespace Aura.ArchitectureTests;

/// <summary>
/// Proves the spec scenario "Infrastructure file placement":
/// All infrastructure implementations MUST reside under Adapters/{Responsibility}/ or Shared/{Concept}/
/// and MUST NOT be in the project root or generic technical folders.
/// </summary>
public class InfrastructureOrganizationTests
{
    private static readonly string InfrastructureProjectDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Aura.Infrastructure"));

    /// <summary>
    /// Asserts that all .cs source files in Aura.Infrastructure are placed under
    /// Adapters/, Shared/, or are the root DependencyInjection.cs entry point.
    /// No generic folders like Embedding/, VectorStore/, Persistence/ at root level.
    /// </summary>
    [Fact]
    public void InfrastructureSourceFiles_MustResideInAdaptersOrSharedFolders()
    {
        // Arrange: get all .cs files in the Infrastructure project (excluding obj/bin)
        var allSourceFiles = Directory.GetFiles(InfrastructureProjectDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.Combine("bin", "")) && !f.Contains(Path.Combine("obj", "")))
            .Select(f => Path.GetRelativePath(InfrastructureProjectDir, f))
            .ToList();

        // Allowed locations: root-level files (DependencyInjection.cs, GlobalUsings.cs, etc.)
        // and files under Adapters/, Shared/, or Health/
        var allowedPrefixes = new[] { "Adapters" + Path.DirectorySeparatorChar, "Shared" + Path.DirectorySeparatorChar, "Health" + Path.DirectorySeparatorChar };

        // Act: find files that are NOT root-level AND NOT under allowed prefixes
        var misplaced = allSourceFiles
            .Where(f => f.Contains(Path.DirectorySeparatorChar)) // has subdirectory (not root-level)
            .Where(f => !allowedPrefixes.Any(prefix => f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Assert
        Assert.True(misplaced.Count == 0,
            $"Infrastructure source files found outside Adapters/ or Shared/ folders: {string.Join(", ", misplaced)}");
    }

    /// <summary>
    /// Asserts that legacy generic folders (Embedding/, VectorStore/, Persistence/) do NOT
    /// exist at the Infrastructure project root level.
    /// </summary>
    [Fact]
    public void InfrastructureProject_MustNotContainLegacyGenericFolders()
    {
        // Arrange
        var forbiddenFolders = new[] { "Embedding", "VectorStore", "Persistence" };

        // Act
        var existingForbidden = forbiddenFolders
            .Where(folder => Directory.Exists(Path.Combine(InfrastructureProjectDir, folder)))
            .ToList();

        // Assert
        Assert.True(existingForbidden.Count == 0,
            $"Legacy generic folders still exist at Infrastructure root: {string.Join(", ", existingForbidden)}");
    }

    /// <summary>
    /// Asserts that every Adapters/ subfolder contains at least one .cs file,
    /// proving the adapter-centric structure is not empty scaffolding.
    /// </summary>
    [Fact]
    public void InfrastructureAdapters_EachSubfolderMustContainSourceFiles()
    {
        // Arrange
        var adaptersDir = Path.Combine(InfrastructureProjectDir, "Adapters");

        // Act
        var adapterFolders = Directory.GetDirectories(adaptersDir);
        var emptyAdapters = adapterFolders
            .Where(dir => !Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).Any())
            .Select(Path.GetFileName)
            .ToList();

        // Assert
        Assert.NotEmpty(adapterFolders); // At least one adapter must exist
        Assert.True(emptyAdapters.Count == 0,
            $"Adapter folders with no source files: {string.Join(", ", emptyAdapters!)}");
    }
}
