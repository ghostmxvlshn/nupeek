namespace Nupeek.Core.Tests;

public class PackageTypeLocatorTests
{
    [Fact]
    public async Task Locate_FindsAssemblyForKnownType()
    {
        var cacheRoot = Path.Combine(Path.GetTempPath(), "nupeek-tests", Guid.NewGuid().ToString("N"));
        var acquirer = new NuGetPackageAcquirer();
        var locator = new PackageTypeLocator();

        try
        {
            var package = await acquirer.AcquireAsync(new NuGetPackageRequest("Humanizer.Core", "2.14.1", cacheRoot));
            var result = await locator.LocateAsync(new PackageContentRequest(package.ExtractedPath, "Humanizer.StringHumanizeExtensions", "netstandard2.0"));

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
        var acquirer = new NuGetPackageAcquirer();
        var locator = new PackageTypeLocator();

        try
        {
            var package = await acquirer.AcquireAsync(new NuGetPackageRequest("Humanizer.Core", "2.14.1", cacheRoot));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await locator.LocateAsync(new PackageContentRequest(package.ExtractedPath, "Humanizer.StringHumanizeExtensions", "net9.0")).ConfigureAwait(true));
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
