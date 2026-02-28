using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace Nupeek.Core;

public sealed class TypeDecompiler
{
    public void DecompileType(string assemblyPath, string fullTypeName, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var settings = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false,
            NullableReferenceTypes = true,
            UsingDeclarations = true,
        };

        var decompiler = new CSharpDecompiler(assemblyPath, settings);
        var syntax = decompiler.DecompileType(new FullTypeName(fullTypeName));

        var directory = Path.GetDirectoryName(outputPath)
            ?? throw new InvalidOperationException("Output path directory is missing.");

        Directory.CreateDirectory(directory);
        File.WriteAllText(outputPath, syntax.ToString());
    }
}
