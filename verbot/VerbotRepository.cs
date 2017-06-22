using System;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path;
using MacroExceptions;
using MacroGit;
using MacroSln;
using MacroGuards;
using Semver;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;


namespace
verbot
{


internal class
VerbotRepository
    : GitRepository
{


public
VerbotRepository(string path)
    : base(path)
{
    var sln = VisualStudioSolution.Find(Path);
    if (sln == null) throw new UserException("No Visual Studio solution found in repository");
    Solution = sln;
}


public VisualStudioSolution
Solution
{
    get;
}


public SemVersion
GetVersion()
{
    var assemblyInfos = FindAssemblyInfos();
    if (assemblyInfos.Count == 0)
        throw new UserException("No [AssemblyInformationalVersion] attributes found in solution");

    var distinctVersions =
        assemblyInfos
            .Select(aiv => aiv.Version)
            .Where(aiv => aiv != null)
            .Distinct()
            .ToList();

    if (distinctVersions.Count > 1)
    {
        Trace.TraceInformation("[AssemblyInformationalVersion] attributes in solution");
        foreach (var aiv in assemblyInfos)
        {
            Trace.TraceInformation("  {0} in {1}", aiv.Version, aiv.Path);
        }
        throw new UserException("Conflicting [AssemblyInformationalVersion] attributes encountered");
    }

    return distinctVersions.Single();
}


public void
SetVersion(SemVersion version)
{
    Guard.NotNull(version, nameof(version));

    var assemblyVersion = FormattableString.Invariant(
        $"{version.Major}.0.0.0");

    var assemblyFileVersion = FormattableString.Invariant(
        $"{version.Major}.{version.Minor}.{version.Patch}.0");

    var assemblyInfoPaths = FindAssemblyInfoPaths();
    if (assemblyInfoPaths.Count == 0)
        throw new UserException("No AssemblyInfo files found in solution");

    int count = 0;
    foreach (var path in assemblyInfoPaths)
    {
        if (
            AssemblyInfoHelper.TrySetAssemblyAttributeValue(
                path,
                "AssemblyInformationalVersion",
                version.ToString()))
        {
            count++;
        }
    }

    if (count == 0)
        throw new UserException("No [AssemblyInformationalVersion] attributes found in AssemblyInfo files");

    foreach (var path in assemblyInfoPaths)
    {
        AssemblyInfoHelper.TrySetAssemblyAttributeValue(path, "AssemblyVersion", assemblyVersion);
        AssemblyInfoHelper.TrySetAssemblyAttributeValue(path, "AssemblyFileVersion", assemblyFileVersion);
    }
}


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
    Justification = "Unclear how to refactor while maintaining clarity")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength",
    Justification = "Comparing to empty string communicates intention more clearly")]
public void
IncrementVersion(bool major, bool minor)
{
    var patch = !(major || minor);

    // Check for no uncommitted changes
    if (HasUncommittedChanges())
        throw new UserException("Uncommitted changes in repository");

    // Get current version
    var version = GetVersion();
    var minorVersion = version.Change(null, null, 0, "", "");
    var minorVersionString = FormattableString.Invariant($"{version.Major}.{version.Minor}");
    var patchVersionString = FormattableString.Invariant($"{version.Major}.{version.Minor}.{version.Patch}");

    // Check for release or -master prerelease
    if (!(version.Prerelease == "" || version.Prerelease == "master"))
        throw new UserException(
            "Expected current version to be a release or -master prerelease, but found " + version.ToString());
        
    // Get branch information
    var currentBranch = GetBranch();
    var currentIsMaster = (currentBranch == "master");
    var masterBranches = FindMasterBranches();

    // Check that the master branch, if present, is tracking the highest version
    if (masterBranches.Any(mb => mb.Name == "master") && masterBranches.First().Name != "master")
        throw new UserException("Expected master branch to be tracking the latest version");

    // Check that we're on the correct master branch for the current version
    var expectedCurrentBranch = masterBranches.Where(mb => mb.Version == minorVersion).Select(mb => mb.Name).Single();
    if (currentBranch != expectedCurrentBranch)
        throw new UserException("Expected to be on branch " + expectedCurrentBranch);

    // Check that we're not skipping over perfectly good unreleased versions
    if (version.Prerelease == "master")
    {
        if (patch)
            throw new UserException(FormattableString.Invariant(
                $"No need to increment patch when {patchVersionString} hasn't been released yet"));
        if (minor && version.Patch == 0)
            throw new UserException(FormattableString.Invariant(
                $"No need to increment minor when {patchVersionString} hasn't been released yet"));
        if (major && version.Minor == 0 && version.Patch == 0)
            throw new UserException(FormattableString.Invariant(
                $"No need to increment major when {minorVersionString} hasn't been released yet"));
    }

    // Compute next version
    var newVersion = version.Change(null, null, null, "master", "");
    if (patch)
    {
        newVersion = newVersion.Change(null, null, newVersion.Patch + 1, null, null);
    }
    else if (minor)
    {
        newVersion = newVersion.Change(null, newVersion.Minor + 1, 0, null, null);
    }
    else if (major)
    {
        newVersion = newVersion.Change(newVersion.Major + 1, 0, 0, null, null);
    }
    var newMinorVersion = newVersion.Change(null, null, 0, "", "");

    // Check that we're not trying to advance to the latest version from a branch other than master
    if (newMinorVersion > masterBranches.First().Version && !currentIsMaster)
        throw new UserException("Must be on master branch to advance to latest version");

    if (major || minor)
    {
        // (master branch) Leave a new MAJOR.MINOR-master branch behind for the current version
        if (currentIsMaster)
        {
            var newBranchVersion = minorVersion;

            if (masterBranches.Any(mb => mb.Name != "master" && mb.Version == newBranchVersion))
                throw new UserException(FormattableString.Invariant(
                    $"A -master branch tracking {newBranchVersion.Major}.{newBranchVersion.Minor} already exists"));

            var newBranch = new GitCommitName(FormattableString.Invariant(
                $"{newBranchVersion.Major}.{newBranchVersion.Minor}-master"));

            Trace.TraceInformation("Creating branch " + newBranch);
            CreateBranch(newBranch);
        }

        // (-master branch) Create and proceed on a new NEWMAJOR.NEWMINOR-master branch
        else
        {
            var newBranchVersion = newMinorVersion;
            if (masterBranches.Any(mb => mb.Version == newBranchVersion))
                throw new UserException(FormattableString.Invariant(
                    $"A master branch tracking {newBranchVersion.Major}.{newBranchVersion.Minor} already exists"));

            var newBranch = new GitCommitName(FormattableString.Invariant(
                $"{newBranchVersion.Major}.{newBranchVersion.Minor}-master"));

            Trace.TraceInformation("Creating and switching to branch " + newBranch);
            CreateBranch(newBranch);
            Checkout(newBranch);
        }
    }

    // Advance to new version
    Trace.TraceInformation("Incrementing to version " + newVersion.ToString() + " on branch " + GetBranch());
    SetVersion(newVersion);
    StageChanges();
    Commit(FormattableString.Invariant($"Increment to version {newVersion}"));
}


IList<AssemblyInfoInfo>
FindAssemblyInfos()
{
    return
        FindAssemblyInfoPaths()
            .Select(path => new {
                Path = path,
                Version = AssemblyInfoHelper.FindAssemblyAttributeValue(path, "AssemblyInformationalVersion") })
            .Where(aiv => aiv.Version != null)
            .Select(aiv => {
                SemVersion version;
                if (!SemVersion.TryParse(aiv.Version, out version))
                    throw new UserException(FormattableString.Invariant(
                        $"[AssemblyInformationalVersion] in {aiv.Path} doesn't contain a valid semver"));
                return new AssemblyInfoInfo(aiv.Path, version);
            })
            .ToList();
}


IList<string>
FindAssemblyInfoPaths()
{
    return
        // All projects
        Solution.ProjectReferences
            // ...that are C# projects
            .Where(pr => pr.TypeId == VisualStudioProjectTypeIds.CSharp)
            // ..."local" to the solution
            .Where(pr => !pr.Location.StartsWith("..", StringComparison.Ordinal))
            .Select(pr => pr.GetProject())
            // ...all their compile items
            .SelectMany(p =>
                p.CompileItems.Select(path =>
                    IOPath.GetFullPath(IOPath.Combine(IOPath.GetDirectoryName(p.Path), path))))
            // ...that are .cs files
            .Where(path => IOPath.GetExtension(path) == ".cs")
            // ...and have "AssemblyInfo" in their name
            .Where(path => IOPath.GetFileNameWithoutExtension(path).Contains("AssemblyInfo"))
            .Distinct()
            .ToList();
}


/// <summary>
/// Find all 'master' and 'MAJOR.MINOR-master' branches and the MAJOR.MINOR versions they track, in decreasing version
/// order
/// </summary>
///
IList<MasterBranchInfo>
FindMasterBranches()
{
    var results = new List<MasterBranchInfo>();

    var branches = GetBranches();
    var currentBranch = GetBranch();
    
    var masterBranch = branches.Where(n => n == "master").FirstOrDefault();
    if (masterBranch != null)
    {
        SemVersion masterVersion = null;
        try
        {
            Checkout(masterBranch);
            masterVersion = GetVersion();
        }
        finally
        {
            Checkout(currentBranch);
        }
        var masterMinorVersion = masterVersion.Change(null, null, 0, "", "");
        results.Add(new MasterBranchInfo(masterBranch, masterMinorVersion));
    }

    results.AddRange(
        branches
            .Select(name => new {
                Name = name,
                Match = Regex.Match(name, @"^(\d+)\.(\d+)-master$") })
            .Where(m => m.Match.Success)
            .Select(m =>
                new MasterBranchInfo(
                    m.Name,
                    new SemVersion(
                        int.Parse(m.Match.Groups[1].Value, CultureInfo.InvariantCulture),
                        int.Parse(m.Match.Groups[2].Value, CultureInfo.InvariantCulture)))));

    results.Sort((x,y) => y.Version.CompareTo(x.Version));

    return results;
}


class
AssemblyInfoInfo
{
    public
    AssemblyInfoInfo(string path, SemVersion version)
    {
        Guard.NotNull(path, nameof(path));
        Path = path;
        Version = version;
    }
    public string Path { get; }
    public SemVersion Version { get; }
}


class
MasterBranchInfo
{
    public
    MasterBranchInfo(GitCommitName name, SemVersion version)
    {
        Name = name;
        Version = version;
    }
    public GitCommitName Name { get; }
    public SemVersion Version { get; }
    public override string ToString() => FormattableString.Invariant($"{Name} -> {Version.Major}.{Version.Minor}");
}


}
}
