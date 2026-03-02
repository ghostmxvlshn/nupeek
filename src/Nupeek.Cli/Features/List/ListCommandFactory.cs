using Nupeek.Core;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace Nupeek.Cli;

internal static class ListCommandFactory
{
    public static Command Create(CancellationToken cancellationToken)
    {
        var packageOption = new Option<string?>("--package", "NuGet package id.");
        packageOption.AddAlias("-p");

        var options = new ListCommandOptions(
            packageOption,
            new Option<string?>("--assembly", "Path to local assembly (.dll)."),
            new Option<string?>("--version", "NuGet package version. Defaults to latest. Ignored with --assembly."),
            new Option<string?>("--tfm", "Target framework moniker. Defaults to auto."),
            new Option<string>("--out", () => "deps-src", "Output/cache directory for package mode."),
            new Option<string?>("--query", "Optional type-name filter (prefix/contains)."),
            new Option<string>("--format", () => "text", "Output format: text (default) or json."));

        var command = new Command("list", "List available types from package or assembly.");
        command.AddOption(options.Package);
        command.AddOption(options.Assembly);
        command.AddOption(options.Version);
        command.AddOption(options.Tfm);
        command.AddOption(options.Out);
        command.AddOption(options.Query);
        command.AddOption(options.Format);

        command.SetHandler(async (InvocationContext context) =>
            await HandleAsync(context, options, cancellationToken).ConfigureAwait(false));

        return command;
    }

    private static async Task HandleAsync(InvocationContext context, ListCommandOptions options, CancellationToken cancellationToken)
    {
        var parse = context.ParseResult;
        var package = parse.GetValueForOption(options.Package);
        var assembly = parse.GetValueForOption(options.Assembly);

        var validation = ValidateSource(package, assembly);
        if (validation is not null)
        {
            Console.Error.WriteLine(validation);
            Environment.ExitCode = ExitCodes.InvalidArguments;
            return;
        }

        var types = await ResolveTypesAsync(parse, options, package, assembly, cancellationToken).ConfigureAwait(false);
        var filtered = FilterTypes(types, parse.GetValueForOption(options.Query));

        WriteOutput(filtered, InputValidation.NormalizeFormat(parse.GetValueForOption(options.Format) ?? "text"));
        Environment.ExitCode = ExitCodes.Success;
    }

    private static async Task<IReadOnlyList<string>> ResolveTypesAsync(
        System.CommandLine.Parsing.ParseResult parse,
        ListCommandOptions options,
        string? package,
        string? assembly,
        CancellationToken cancellationToken)
    {
        var locator = new PackageTypeLocator();

        if (!string.IsNullOrWhiteSpace(assembly))
        {
            return await locator.ListTypesInAssemblyAsync(assembly!, cancellationToken).ConfigureAwait(false);
        }

        var cacheRoot = Path.Combine(parse.GetValueForOption(options.Out)!, ".cache");
        var acquirer = new NuGetPackageAcquirer();
        var packageResult = await acquirer.AcquireAsync(new NuGetPackageRequest(
            package!,
            parse.GetValueForOption(options.Version),
            cacheRoot), cancellationToken).ConfigureAwait(false);

        return await locator.ListTypesInPackageAsync(packageResult.ExtractedPath, parse.GetValueForOption(options.Tfm), cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<string> FilterTypes(IReadOnlyList<string> types, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return types;
        }

        var clean = query.Trim();
        return types.Where(type =>
                type.Contains(clean, StringComparison.OrdinalIgnoreCase)
                || type.Split('.').Last().StartsWith(clean, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static void WriteOutput(IReadOnlyList<string> types, string format)
    {
        if (string.Equals(format, "json", StringComparison.Ordinal))
        {
            Console.WriteLine(JsonSerializer.Serialize(types, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        foreach (var type in types)
        {
            Console.WriteLine(type);
        }

        Console.Error.WriteLine($"Listed {types.Count} types.");
    }

    private static string? ValidateSource(string? package, string? assembly)
    {
        if (string.IsNullOrWhiteSpace(package) && string.IsNullOrWhiteSpace(assembly))
        {
            return "Provide exactly one source: --package <id> or --assembly <path-to-dll>.";
        }

        if (!string.IsNullOrWhiteSpace(package) && !string.IsNullOrWhiteSpace(assembly))
        {
            return "Use either --package or --assembly, not both.";
        }

        return null;
    }

    private sealed record ListCommandOptions(
        Option<string?> Package,
        Option<string?> Assembly,
        Option<string?> Version,
        Option<string?> Tfm,
        Option<string> Out,
        Option<string?> Query,
        Option<string> Format);
}
