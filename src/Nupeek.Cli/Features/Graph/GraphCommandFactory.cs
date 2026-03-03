using Nupeek.Core;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace Nupeek.Cli;

internal static class GraphCommandFactory
{
    public static Command Create(CancellationToken cancellationToken)
    {
        var assemblyOption = new Option<string>("--assembly", "Path to local assembly (.dll)") { IsRequired = true };
        var typeOption = new Option<string?>("--type", "Optional root type to scope traversal.");
        var depthOption = new Option<int>("--depth", () => 1, "Traversal depth from root type. Ignored when --type omitted.");
        var outOption = new Option<string>("--out", "Output directory (e.g. deps-src)") { IsRequired = true };

        var command = new Command("graph", "Export structural internals graph (types, members, edges, globals). Provider-only output.");
        command.AddOption(assemblyOption);
        command.AddOption(typeOption);
        command.AddOption(depthOption);
        command.AddOption(outOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var parse = context.ParseResult;
            var assembly = parse.GetValueForOption(assemblyOption)!;
            var rootType = parse.GetValueForOption(typeOption);
            var depth = Math.Max(0, parse.GetValueForOption(depthOption));
            var outDir = parse.GetValueForOption(outOption)!;

            var builder = new AssemblyGraphBuilder();
            var graph = await builder.BuildAsync(assembly, rootType, depth, cancellationToken).ConfigureAwait(false);

            Directory.CreateDirectory(outDir);

            var json = new JsonSerializerOptions { WriteIndented = true };
            var typesPath = Path.Combine(outDir, "graph.types.json");
            var membersPath = Path.Combine(outDir, "graph.members.json");
            var edgesPath = Path.Combine(outDir, "graph.edges.json");
            var globalsPath = Path.Combine(outDir, "graph.globals.json");

            await File.WriteAllTextAsync(typesPath, JsonSerializer.Serialize(graph.Types, json), cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(membersPath, JsonSerializer.Serialize(graph.Members, json), cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(edgesPath, JsonSerializer.Serialize(graph.Edges, json), cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(globalsPath, JsonSerializer.Serialize(graph.Globals, json), cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"typesPath: {typesPath}");
            Console.WriteLine($"membersPath: {membersPath}");
            Console.WriteLine($"edgesPath: {edgesPath}");
            Console.WriteLine($"globalsPath: {globalsPath}");
            Console.Error.WriteLine($"Graph export complete: {graph.Types.Count} types, {graph.Members.Count} members, {graph.Edges.Count} edges, {graph.Globals.Count} globals.");

            Environment.ExitCode = ExitCodes.Success;
        });

        return command;
    }
}
