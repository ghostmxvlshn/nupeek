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
