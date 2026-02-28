namespace Nupeek.Core.Tests;

public class NuGetCacheLayoutTests
{
    [Fact]
    public void PackageDirectory_UsesDeterministicLayout()
    {
        var path = NuGetCacheLayout.PackageDirectory("deps-src/.cache", "Newtonsoft.Json", "13.0.3");
        Assert.EndsWith(Path.Combine("deps-src", ".cache", "packages", "newtonsoft.json", "13.0.3"), path, StringComparison.Ordinal);
    }
}
