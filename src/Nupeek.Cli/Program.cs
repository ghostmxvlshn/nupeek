using Nupeek.Core;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var command = args[0].ToLowerInvariant();
if (command is not ("type" or "find"))
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintUsage();
    return 2;
}

var map = ParseArgs(args.Skip(1).ToArray());

if (!map.TryGetValue("package", out var package) || string.IsNullOrWhiteSpace(package))
    return Fail("--package is required");

if (!map.TryGetValue("out", out var outDir) || string.IsNullOrWhiteSpace(outDir))
    return Fail("--out is required");

var version = map.TryGetValue("version", out var v) ? v : "latest";
var tfm = map.TryGetValue("tfm", out var t) ? t : "auto";
var type = command == "type"
    ? map.GetValueOrDefault("type")
    : SymbolParser.ToTypeName(map.GetValueOrDefault("symbol") ?? string.Empty);

if (string.IsNullOrWhiteSpace(type))
    return Fail(command == "type" ? "--type is required" : "--symbol is required");

Console.WriteLine("Nupeek dry-run");
Console.WriteLine($"command: {command}");
Console.WriteLine($"package: {package}");
Console.WriteLine($"version: {version}");
Console.WriteLine($"tfm: {tfm}");
Console.WriteLine($"type: {type}");
Console.WriteLine($"out: {outDir}");

return 0;

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 3;
}

static Dictionary<string, string> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < args.Length; i++)
    {
        var key = args[i];
        if (!key.StartsWith("--", StringComparison.Ordinal))
            continue;

        var normalizedKey = key[2..];
        var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
            ? args[++i]
            : "true";

        result[normalizedKey] = value;
    }

    return result;
}

static void PrintUsage()
{
    Console.WriteLine("nupeek type --package <id> [--version <v>] [--tfm <tfm>] --type <Namespace.Type> --out <dir>");
    Console.WriteLine("nupeek find --package <id> [--version <v>] [--tfm <tfm>] --symbol <Namespace.Type.Method> --out <dir>");
}
