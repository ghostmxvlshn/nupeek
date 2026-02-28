using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace Nupeek.Core;

/// <summary>
/// Decompiles a single CLR type from an assembly into C# source.
/// </summary>
public sealed class TypeDecompiler
{
    /// <summary>
    /// Decompiles the requested type and writes the generated source to disk.
    /// </summary>
    public void DecompileType(string assemblyPath, string fullTypeName, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        // Conservative defaults prioritize resilience over strict reference resolution.
        var settings = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false,
            NullableReferenceTypes = true,
            UsingDeclarations = true,
        };

        // Build decompiler for target assembly and type.
        var decompiler = new CSharpDecompiler(assemblyPath, settings);
        var syntax = decompiler.DecompileType(new FullTypeName(fullTypeName));

        // Ensure destination folder exists before writing source output.
        var directory = Path.GetDirectoryName(outputPath)
            ?? throw new InvalidOperationException("Output path directory is missing.");

        Directory.CreateDirectory(directory);
        File.WriteAllText(outputPath, syntax.ToString());
    }
}
