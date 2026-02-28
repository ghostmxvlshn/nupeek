namespace Nupeek.Core.Tests;

public class NuGetPackageAcquirerTests
{
    [Fact]
    public async Task AcquireAsync_DownloadsAndExtractsPackage()
    {
        // Arrange
        var cacheRoot = Path.Combine(Path.GetTempPath(), "nupeek-tests", Guid.NewGuid().ToString("N"));
        var acquirer = new NuGetPackageAcquirer();

        try
        {
            // Act
            var result = await acquirer.AcquireAsync(new NuGetPackageRequest("Humanizer.Core", "2.14.1", cacheRoot));

            // Assert
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
