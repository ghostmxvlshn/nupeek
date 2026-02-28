using System.Text.Json;

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
    public async Task RunAsync_TypeDryRun_JsonFormat_WritesJsonPayload()
    {
        // Arrange
        var args = new[]
        {
            "type",
            "--package", "Polly",
            "--type", "Polly.Policy",
            "--out", "deps-src",
            "--format", "json",
        };

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            // Act
            var code = await CliApp.RunAsync(args);

            // Assert
            Assert.Equal(ExitCodes.Success, code);
            var payload = JsonDocument.Parse(writer.ToString());
            Assert.Equal("type", payload.RootElement.GetProperty("Command").GetString());
            Assert.Equal("Polly", payload.RootElement.GetProperty("PackageId").GetString());
            Assert.Equal("Polly.Policy", payload.RootElement.GetProperty("InputType").GetString());
            Assert.Equal(0, payload.RootElement.GetProperty("ExitCode").GetInt32());
            Assert.True(payload.RootElement.GetProperty("DryRun").GetBoolean());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task RunAsync_TypeFailure_JsonFormat_WritesJsonErrorPayload()
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
            "--format", "json",
        };

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            // Act
            var code = await CliApp.RunAsync(args);

            // Assert
            Assert.Equal(ExitCodes.DecompilationFailure, code);
            var payload = JsonDocument.Parse(writer.ToString());
            Assert.Equal(ExitCodes.DecompilationFailure, payload.RootElement.GetProperty("ExitCode").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(payload.RootElement.GetProperty("Error").GetString()));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
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
