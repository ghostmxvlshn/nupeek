namespace Nupeek.Core.Tests;

public class NuGetCacheLayoutTests
{
    [Fact]
    public void PackageDirectory_UsesDeterministicLayout()
    {
        // Arrange
        const string cacheRoot = "deps-src/.cache";

        // Act
        var path = NuGetCacheLayout.PackageDirectory(cacheRoot, "Newtonsoft.Json", "13.0.3");

        // Assert
        Assert.EndsWith(Path.Combine("deps-src", ".cache", "packages", "newtonsoft.json", "13.0.3"), path, StringComparison.Ordinal);
    }
}
