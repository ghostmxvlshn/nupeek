using System.IO.Compression;

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

    [Fact]
    public async Task AcquireAsync_InvalidPackageId_ThrowsAndDoesNotCreateCacheFolders()
    {
        // Arrange
        var cacheRoot = Path.Combine(Path.GetTempPath(), "nupeek-tests", Guid.NewGuid().ToString("N"));
        var acquirer = new NuGetPackageAcquirer();

        try
        {
            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                acquirer.AcquireAsync(new NuGetPackageRequest("../../pwned", "1.0.0", cacheRoot)));

            Assert.False(Directory.Exists(cacheRoot));
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
    public async Task AcquireAsync_ZipSlipEntry_Throws()
    {
        // Arrange
        var cacheRoot = Path.Combine(Path.GetTempPath(), "nupeek-tests", Guid.NewGuid().ToString("N"));
        var packageId = "Safe.Package";
        var version = "1.0.0";

        var packageDir = NuGetCacheLayout.PackageDirectory(cacheRoot, packageId, version);
        var nupkgPath = NuGetCacheLayout.NupkgPath(cacheRoot, packageId, version);
        var acquirer = new NuGetPackageAcquirer();

        Directory.CreateDirectory(packageDir);
        using (var archive = ZipFile.Open(nupkgPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("../evil.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("boom");
        }

        try
        {
            // Act + Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                acquirer.AcquireAsync(new NuGetPackageRequest(packageId, version, cacheRoot)));

            Assert.Contains("Unsafe archive entry path", ex.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(Path.Combine(cacheRoot, "evil.txt")));
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
