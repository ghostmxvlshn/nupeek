namespace Nupeek.Cli.Tests;

public class CliAppTests
{
    [Fact]
    public async Task RunAsync_UnknownCommand_ReturnsInvalidArguments()
    {
        // Arrange
        var args = new[] { "wat" };

        // Act
        var code = await CliApp.RunAsync(args);

        // Assert
        Assert.Equal(ExitCodes.InvalidArguments, code);
    }

    [Fact]
    public async Task RunAsync_Help_ReturnsSuccess()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var code = await CliApp.RunAsync(args);

        // Assert
        Assert.Equal(ExitCodes.Success, code);
    }

    [Fact]
    public async Task RunAsync_TypeCommandFailure_ReturnsNonZeroExitCode()
    {
        // Arrange
        var outDir = Path.Combine(Path.GetTempPath(), $"nupeek-cli-test-{Guid.NewGuid():N}");
        var args = new[]
        {
            "type",
            "--package", "../../pwned",
            "--version", "1.0.0",
            "--type", "A.B",
            "--out", outDir,
            "--dry-run", "false",
            "--quiet",
        };

        // Act
        var code = await CliApp.RunAsync(args);

        // Assert
        Assert.Equal(ExitCodes.DecompilationFailure, code);
    }

    [Fact]
    public void BuildPlanText_IncludesSymbolWhenProvided()
    {
        // Arrange
        const string sourceSymbol = "Polly.Policy.Handle";

        // Act
        var text = CliApp.BuildPlanText(
            command: "find",
            package: "Polly",
            version: "latest",
            tfm: "auto",
            type: "Polly.Policy",
            outDir: "deps-src",
            dryRun: true,
            sourceSymbol: sourceSymbol);

        // Assert
        Assert.Contains("symbol: Polly.Policy.Handle", text, StringComparison.Ordinal);
        Assert.Contains("type: Polly.Policy", text, StringComparison.Ordinal);
    }
}
