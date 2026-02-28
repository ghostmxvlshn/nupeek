namespace Nupeek.Core.Tests;

public class PackageTypeLocatorTests
{
    [Fact]
    public async Task Locate_FindsAssemblyForKnownType()
    {
        var cacheRoot = Path.Combine(Path.GetTempPath(), "nupeek-tests", Guid.NewGuid().ToString("N"));

        try
        {
            var acquirer = new NuGetPackageAcquirer();
            var package = await acquirer.AcquireAsync(new NuGetPackageRequest("Humanizer.Core", "2.14.1", cacheRoot));

            var locator = new PackageTypeLocator();
            var result = locator.Locate(new PackageContentRequest(
                package.ExtractedPath,
                "Humanizer.StringHumanizeExtensions",
                "netstandard2.0"));

            Assert.Equal("netstandard2.0", result.SelectedTfm);
            Assert.True(File.Exists(result.AssemblyPath));
        }
        finally
        {
            if (Directory.Exists(cacheRoot))
            {
                Directory.Delete(cacheRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Locate_ThrowsForMissingTfm()
    {
        var cacheRoot = Path.Combine(Path.GetTempPath(), "nupeek-tests", Guid.NewGuid().ToString("N"));

        try
        {
            var acquirer = new NuGetPackageAcquirer();
            var package = await acquirer.AcquireAsync(new NuGetPackageRequest("Humanizer.Core", "2.14.1", cacheRoot));
            var locator = new PackageTypeLocator();

            Assert.Throws<InvalidOperationException>(() =>
                locator.Locate(new PackageContentRequest(package.ExtractedPath, "Humanizer.StringHumanizeExtensions", "net9.0")));
        }
        finally
        {
            if (Directory.Exists(cacheRoot))
            {
                Directory.Delete(cacheRoot, recursive: true);
            }
        }
    }
}
