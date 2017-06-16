using MacroGuards;
using MacroExceptions;
using Semver;
using System;

namespace
verbot
{


internal static class
SetCommand
{


public static void
Set(VerbotRepository repository, SemVersion version)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(version, nameof(version));

    var assemblyVersion = FormattableString.Invariant(
        $"{version.Major}.0.0.0");

    var assemblyFileVersion = FormattableString.Invariant(
        $"{version.Major}.{version.Minor}.{version.Patch}.0");

    var assemblyInfos = repository.FindAssemblyInfos();
    if (assemblyInfos.Count == 0)
        throw new UserException("No AssemblyInfo files found in solution");

    int count = 0;
    foreach (var assemblyInfo in assemblyInfos)
    {
        if (
            VerbotRepository.TrySetAssemblyAttributeValue(
                assemblyInfo,
                "AssemblyInformationalVersion",
                version.ToString()))
        {
            count++;
        }
    }

    if (count == 0)
        throw new UserException("No [AssemblyInformationalVersion] attributes found in AssemblyInfo files");

    foreach (var assemblyInfo in assemblyInfos)
    {
        VerbotRepository.TrySetAssemblyAttributeValue(assemblyInfo, "AssemblyVersion", assemblyVersion);
        VerbotRepository.TrySetAssemblyAttributeValue(assemblyInfo, "AssemblyFileVersion", assemblyFileVersion);
    }
}


}
}
