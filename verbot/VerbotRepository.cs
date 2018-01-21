using System;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path;
using MacroExceptions;
using MacroGit;
using MacroGuards;
using MacroSemver;
using MacroSln;
using System.Diagnostics;


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
    var assemblyInfoFiles = FindAssemblyInfoFiles();
    if (assemblyInfoFiles.Count == 0)
        throw new UserException("No AssemblyInfo files found in solution");

    var distinctVersions =
        assemblyInfoFiles
            .Select(f => f.FindVersion())
            .Where(v => v != null)
            .Distinct()
            .ToList();

    if (distinctVersions.Count == 0)
        throw new UserException("No [AssemblyInformationalVersion] attributes found in solution");

    if (distinctVersions.Count > 1)
    {
        Trace.TraceInformation("[AssemblyInformationalVersion] attributes in solution");
        foreach (var f in assemblyInfoFiles)
        {
            Trace.TraceInformation("  {0} in {1}", f.FindVersion(), f.Path);
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

    var assemblyInfoFiles = FindAssemblyInfoFiles();
    if (assemblyInfoFiles.Count == 0)
        throw new UserException("No AssemblyInfo files found in solution");

    int count = 0;
    foreach (var f in assemblyInfoFiles)
    {
        if (f.TrySetAssemblyAttributeValue("AssemblyInformationalVersion", version.ToString())) count++;
    }

    if (count == 0)
        throw new UserException("No [AssemblyInformationalVersion] attributes found in AssemblyInfo files");

    foreach (var f in assemblyInfoFiles)
    {
        f.TrySetAssemblyAttributeValue("AssemblyVersion", assemblyVersion);
        f.TrySetAssemblyAttributeValue("AssemblyFileVersion", assemblyFileVersion);
    }
}


public void
IncrementVersion(bool major, bool minor)
{
    CheckNoUncommittedChanges();
    CheckMasterBranchIsTrackingHighestVersion();
    CheckVersionIsReleaseOrMasterPrerelease();
    CheckOnCorrectMasterBranchForVersion();

    var currentVersion = GetVersion();
    var currentMinorVersion = currentVersion.Change(null, null, 0, "", "");
    var currentBranch = GetBranch();
    var onMaster = (currentBranch == "master");
    var masterBranches = FindMasterBranches();
    var nextVersion = CalculateNextVersion(major, minor);
    var nextMinorVersion = nextVersion.Change(null, null, 0, "", "");
    CheckNotAdvancingToLatestVersionOnNonMasterBranch(nextVersion);
    CheckNotSkippingRelease(major, minor);

    // If incrementing major or minor, create and (if necessary) switch to new MAJOR.MINOR-master branch
    if (major || minor)
    {
        if (onMaster)
        {
            var newBranchVersion = currentMinorVersion;

            if (masterBranches.Any(mb => mb.Name != "master" && mb.Version == newBranchVersion))
                throw new UserException(FormattableString.Invariant(
                    $"A -master branch tracking {newBranchVersion.Major}.{newBranchVersion.Minor} already exists"));

            var newBranch = new GitCommitName(FormattableString.Invariant(
                $"{newBranchVersion.Major}.{newBranchVersion.Minor}-master"));

            Trace.TraceInformation("Creating branch " + newBranch);
            CreateBranch(newBranch);
        }
        else
        {
            var newBranchVersion = nextMinorVersion;

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

    // Set version and commit
    var incrementedComponent = major ? "major" : minor ? "minor" : "patch";
    Trace.TraceInformation(FormattableString.Invariant(
        $"Incrementing {incrementedComponent} version to {nextVersion} and committing"));
    SetVersion(nextVersion);
    StageChanges();
    Commit(FormattableString.Invariant($"Increment {incrementedComponent} version to {nextVersion}"));
}


public void
Release()
{
    CheckNoUncommittedChanges();
    CheckMasterBranchIsTrackingHighestVersion();
    CheckVersionIsMasterPrerelease();
    CheckOnCorrectMasterBranchForVersion();
    CheckVersionHasNotBeenReleased();

    // Set release version and commit
    var version = GetVersion().Change(null, null, null, "", "");
    Trace.TraceInformation(FormattableString.Invariant($"Setting version to {version} and committing"));
    SetVersion(version);
    StageChanges();
    Commit(FormattableString.Invariant($"Release version {version.ToString()}"));

    // Tag MAJOR.MINOR.PATCH
    Trace.TraceInformation("Tagging " + version.ToString());
    CreateTag(new GitCommitName(version.ToString()));

    // Set MAJOR.MINOR-latest branch
    var majorMinorLatestBranch = FormattableString.Invariant($"{version.Major}.{version.Minor}-latest");
    Trace.TraceInformation(FormattableString.Invariant($"Setting branch {majorMinorLatestBranch}"));
    CreateOrMoveBranch(new GitCommitName(majorMinorLatestBranch));

    // Set MAJOR-latest branch
    var minorVersion = version.Change(null, null, 0, "", "");
    var latestMajorMinorLatestBranch =
        FindMajorMinorLatestBranches().Where(b => b.Version.Major == version.Major).First();
    var isLatestMajorMinorLatestBranch = minorVersion >= latestMajorMinorLatestBranch.Version;
    if (isLatestMajorMinorLatestBranch)
    {
        var majorLatestBranch = FormattableString.Invariant($"{version.Major}-latest");
        Trace.TraceInformation(FormattableString.Invariant($"Setting branch {majorLatestBranch}"));
        CreateOrMoveBranch(new GitCommitName(majorLatestBranch));
    }

    // Set latest branch
    var majorVersion = version.Change(null, 0, 0, "", "");
    var latestMajorLatestBranch = FindMajorLatestBranches().First();
    var isLatestMajorLatestBranch = majorVersion >= latestMajorLatestBranch.Version;
    if (isLatestMajorMinorLatestBranch && isLatestMajorLatestBranch)
    {
        Trace.TraceInformation(FormattableString.Invariant($"Setting branch latest"));
        CreateOrMoveBranch(new GitCommitName("latest"));
    }

    // Increment to next patch version
    IncrementVersion(false, false);
}


void
CheckNoUncommittedChanges()
{
    if (HasUncommittedChanges())
        throw new UserException("Uncommitted changes in repository");
}


void
CheckVersionHasNotBeenReleased()
{
    var releaseVersion = GetVersion().Change(null, null, null, "", "");
    if (GetTags().Contains(new GitCommitName(releaseVersion.ToString())))
        throw new UserException("Current version has already been released");
}


void
CheckVersionIsMasterPrerelease()
{
    var version = GetVersion();
    if (version.Prerelease != "master")
        throw new UserException("Expected current version to be a -master prerelease");
}


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength",
    Justification = "Comparing to empty string communicates intention more clearly")]
void
CheckVersionIsReleaseOrMasterPrerelease()
{
    var version = GetVersion();
    if (!(version.Prerelease == "" || version.Prerelease == "master"))
        throw new UserException("Expected current version to be a release or -master prerelease");
}


void
CheckMasterBranchIsTrackingHighestVersion()
{
    var masterBranches = FindMasterBranches();
    if (masterBranches.Any(mb => mb.Name == "master") && masterBranches.First().Name != "master")
        throw new UserException("Expected master branch to be tracking the latest version");
}


void
CheckOnCorrectMasterBranchForVersion()
{
    var minorVersion = GetVersion().Change(null, null, 0, "", "");
    var expectedCurrentBranch =
        FindMasterBranches()
            .Where(mb => mb.Version == minorVersion)
            .Select(mb => mb.Name)
            .SingleOrDefault();
    if (expectedCurrentBranch == null)
        throw new UserException("No master branch found for current version");
    if (GetBranch() != expectedCurrentBranch)
        throw new UserException("Expected to be on branch " + expectedCurrentBranch);
}


void
CheckNotSkippingRelease(bool major, bool minor)
{
    var patch = !(major || minor);
    var version = GetVersion();
    if (version.Prerelease != "master") return;
    var patchString = FormattableString.Invariant($"{version.Major}.{version.Minor}.{version.Patch}");
    var minorString = FormattableString.Invariant($"{version.Major}.{version.Minor}");
    if (patch)
        throw new UserException(FormattableString.Invariant(
            $"No need to increment patch when {patchString} hasn't been released yet"));
    if (minor && version.Patch == 0)
        throw new UserException(FormattableString.Invariant(
            $"No need to increment minor when {patchString} hasn't been released yet"));
    if (major && version.Minor == 0 && version.Patch == 0)
        throw new UserException(FormattableString.Invariant(
            $"No need to increment major when {minorString} hasn't been released yet"));
}


SemVersion
CalculateNextVersion(bool major, bool minor)
{
    var v = GetVersion().Change(null, null, null, "master", "");
    return
        major ?
            v.Change(v.Major + 1, 0, 0, null, null)
        : minor ?
            v.Change(null, v.Minor + 1, 0, null, null)
        :
            v.Change(null, null, v.Patch + 1, null, null);
}


void
CheckNotAdvancingToLatestVersionOnNonMasterBranch(SemVersion newVersion)
{
    var masterBranches = FindMasterBranches();
    if (masterBranches.Count == 0) return;
    var newMinorVersion = newVersion.Change(null, null, 0, "", "");
    if (newMinorVersion > masterBranches.First().Version && GetBranch() != "master")
        throw new UserException("Must be on master branch to advance to latest version");
}


IList<AssemblyInfoFile>
FindAssemblyInfoFiles()
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
            .Select(path => new AssemblyInfoFile(path))
            .ToList();
}


/// <summary>
/// Find information about all 'master' and 'MAJOR.MINOR-master' branches, in decreasing order of tracked minor version
/// </summary>
///
IList<MasterBranchInfo>
FindMasterBranches()
{
    return
        GetBranches()
            .Where(name => MasterBranchInfo.IsMasterBranchName(name))
            .Select(name => new MasterBranchInfo(this, name))
            .OrderByDescending(b => b.Version)
            .ToList();
}


/// <summary>
/// Find information about all 'MAJOR-latest' branches, in decreasing version order
/// </summary>
///
IList<MajorLatestBranchInfo>
FindMajorLatestBranches()
{
    return
        GetBranches()
            .Where(name => MajorLatestBranchInfo.IsMajorLatestBranchName(name))
            .Select(name => new MajorLatestBranchInfo(this, name))
            .OrderByDescending(b => b.Version)
            .ToList();
}


/// <summary>
/// Find information about all 'MAJOR.MINOR-latest' branches, in decreasing version order
/// </summary>
///
IList<MajorMinorLatestBranchInfo>
FindMajorMinorLatestBranches()
{
    return
        GetBranches()
            .Where(name => MajorMinorLatestBranchInfo.IsMajorMinorLatestBranchName(name))
            .Select(name => new MajorMinorLatestBranchInfo(this, name))
            .OrderByDescending(b => b.Version)
            .ToList();
}



}
}
