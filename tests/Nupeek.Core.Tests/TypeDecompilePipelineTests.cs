namespace Nupeek.Core.Tests;

public class TypeDecompilePipelineTests
{
    [Fact]
    public async Task RunAsync_WritesDecompiledTypeAndCatalogFiles()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), "nupeek-tests", Guid.NewGuid().ToString("N"), "deps-src");

        try
        {
            var pipeline = new TypeDecompilePipeline();
            var result = await pipeline.RunAsync(new TypeDecompileRequest(
                "Humanizer.Core",
                "2.14.1",
                "netstandard2.0",
                "Humanizer.StringHumanizeExtensions",
                outputRoot));

            Assert.True(File.Exists(result.OutputPath));
            Assert.True(File.Exists(result.IndexPath));
            Assert.True(File.Exists(result.ManifestPath));

            var indexJson = await File.ReadAllTextAsync(result.IndexPath);
            Assert.Contains("Humanizer.StringHumanizeExtensions", indexJson, StringComparison.Ordinal);

            var manifestJson = await File.ReadAllTextAsync(result.ManifestPath);
            Assert.Contains("Humanizer.Core", manifestJson, StringComparison.Ordinal);
        }
        finally
        {
            var root = Directory.GetParent(outputRoot)?.Parent?.FullName ?? outputRoot;
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
