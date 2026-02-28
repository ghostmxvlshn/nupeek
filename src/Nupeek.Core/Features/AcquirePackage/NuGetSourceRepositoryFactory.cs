using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Nupeek.Core;

internal static class NuGetSourceRepositoryFactory
{
    public static IReadOnlyList<SourceRepository> Create()
    {
        var providers = Repository.Provider.GetCoreV3();
        var packageSources = Settings.LoadDefaultSettings(root: null)
            .GetSection("packageSources")?
            .Items?
            .OfType<SourceItem>()
            .Select(x => new PackageSource(x.GetValueAsPath()))
            .ToList();

        packageSources ??= [];

        if (!packageSources.Any(static x => x.Source.Contains("nuget.org", StringComparison.OrdinalIgnoreCase)))
        {
            packageSources.Add(new PackageSource("https://api.nuget.org/v3/index.json"));
        }

        return packageSources.Select(source => new SourceRepository(source, providers)).ToList();
    }
}
