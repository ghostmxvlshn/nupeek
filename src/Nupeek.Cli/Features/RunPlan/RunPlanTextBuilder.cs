using System.Text;

namespace Nupeek.Cli;

internal static class RunPlanTextBuilder
{
    public static string Build(
        string command,
        string package,
        string version,
        string tfm,
        string type,
        string outDir,
        bool dryRun,
        string? sourceSymbol = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Nupeek execution plan");
        sb.AppendLine($"command: {command}");
        sb.AppendLine($"package: {package}");
        sb.AppendLine($"version: {version}");
        sb.AppendLine($"tfm: {tfm}");

        if (!string.IsNullOrWhiteSpace(sourceSymbol))
        {
            sb.AppendLine($"symbol: {sourceSymbol}");
        }

        sb.AppendLine($"type: {type}");
        sb.AppendLine($"out: {outDir}");
        sb.AppendLine($"dryRun: {dryRun}");

        return sb.ToString().TrimEnd();
    }
}
