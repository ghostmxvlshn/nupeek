namespace Nupeek.Cli.Tests;

public class CliAppTests
{
    [Fact]
    public async Task RunAsync_UnknownCommand_ReturnsInvalidArguments()
    {
        var code = await CliApp.RunAsync(["wat"]);
        Assert.Equal(ExitCodes.InvalidArguments, code);
    }

    [Fact]
    public async Task RunAsync_Help_ReturnsSuccess()
    {
        var code = await CliApp.RunAsync(["--help"]);
        Assert.Equal(ExitCodes.Success, code);
    }

    [Fact]
    public async Task RunAsync_TypeCommandFailure_ReturnsNonZeroExitCode()
    {
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

        var code = await CliApp.RunAsync(args);
        Assert.Equal(ExitCodes.DecompilationFailure, code);
    }

    [Fact]
    public async Task RunAsync_TypeDryRun_WritesTextPlan()
    {
        var args = new[]
        {
            "type",
            "--package", "Polly",
            "--type", "Polly.Policy",
            "--out", "deps-src",
            "--dry-run", "true",
        };

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var code = await CliApp.RunAsync(args);
            Assert.Equal(ExitCodes.Success, code);
            var output = writer.ToString();
            Assert.Contains("Nupeek execution plan", output, StringComparison.Ordinal);
            Assert.Contains("command: type", output, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task RunAsync_InvalidProgressValue_ReturnsGenericError()
    {
        var args = new[]
        {
            "type",
            "--package", "Polly",
            "--type", "Polly.Policy",
            "--out", "deps-src",
            "--progress", "fast",
        };

        var code = await CliApp.RunAsync(args);
        Assert.Equal(ExitCodes.GenericError, code);
    }

    [Fact]
    public void BuildPlanText_IncludesSymbolWhenProvided()
    {
        var text = CliApp.BuildPlanText(
            command: "find",
            package: "Polly",
            version: "latest",
            tfm: "auto",
            type: "Polly.Policy",
            outDir: "deps-src",
            dryRun: true,
            sourceSymbol: "Polly.Policy.Handle");

        Assert.Contains("symbol: Polly.Policy.Handle", text, StringComparison.Ordinal);
        Assert.Contains("type: Polly.Policy", text, StringComparison.Ordinal);
    }
}
