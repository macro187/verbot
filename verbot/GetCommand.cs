using System.Collections.Generic;
using System.Linq;
using MacroGuards;
using MacroExceptions;
using Semver;
using System.Diagnostics;
using MacroSystem;


namespace
verbot
{


internal static class
GetCommand
{


public static SemVersion
Get(VerbotRepository repository)
{
    Guard.NotNull(repository, nameof(repository));

    var assemblyInfos = repository.FindAssemblyInfos();
    if (assemblyInfos.Count == 0)
        throw new UserException("No AssemblyInfo files found in solution");

    var assemblyInfoVersions = FindAssemblyInfoVersions(assemblyInfos);
    if (assemblyInfoVersions.Count == 0)
        throw new UserException("No [AssemblyInformationalVersion] attributes found in solution");

    var distinctVersions =
        assemblyInfoVersions
            .Select(aiv => aiv.Version)
            .Distinct()
            .ToList();

    if (distinctVersions.Count > 1)
    {
        Trace.TraceInformation("[AssemblyInformationalVersion] attributes in solution");
        foreach (var aiv in assemblyInfoVersions)
        {
            Trace.TraceInformation("  {0} in {1}", aiv.Version, aiv.Path);
        }
        throw new UserException("Conflicting [AssemblyInformationalVersion] attributes encountered");
    }

    return distinctVersions.Single();
}


static IList<AssemblyInfoVersion>
FindAssemblyInfoVersions(IEnumerable<string> assemblyInfos)
{
    return
        assemblyInfos
            .Select(path => new {
                Path = path,
                Version = VerbotRepository.FindAssemblyAttributeValue(path, "AssemblyInformationalVersion") })
            .Where(aiv => aiv.Version != null)
            .Select(aiv => {
                SemVersion version;
                if (!SemVersion.TryParse(aiv.Version, out version))
                    throw new UserException(StringExtensions.FormatInvariant(
                        "[AssemblyInformationalVersion] in {0} doesn't contain a valid semver",
                        aiv.Path));
                return new AssemblyInfoVersion(aiv.Path, version);
            })
            .ToList();
}


class
AssemblyInfoVersion
{
    public
    AssemblyInfoVersion(string path, SemVersion version)
    {
        Guard.NotNull(path, nameof(path));
        Path = path;
        Version = version;
    }
    public string Path { get; }
    public SemVersion Version { get; }
}


}
}
