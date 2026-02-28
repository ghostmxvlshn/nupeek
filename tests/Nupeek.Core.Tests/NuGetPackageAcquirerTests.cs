namespace Nupeek.Core.Tests;

public class NuGetPackageAcquirerTests
{
    [Fact]
    public async Task AcquireAsync_DownloadsAndExtractsPackage()
    {
        var cacheRoot = Path.Combine(Path.GetTempPath(), "nupeek-tests", Guid.NewGuid().ToString("N"));

        try
        {
            var acquirer = new NuGetPackageAcquirer();
            var result = await acquirer.AcquireAsync(new NuGetPackageRequest("Humanizer.Core", "2.14.1", cacheRoot));

            Assert.True(File.Exists(result.NupkgPath));
            Assert.True(Directory.Exists(result.ExtractedPath));
            Assert.Equal("2.14.1", result.Version);
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
