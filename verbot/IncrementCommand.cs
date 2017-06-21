using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MacroExceptions;
using MacroGit;
using Semver;


namespace
verbot
{


internal static class
IncrementCommand
{


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
    Justification = "Unclear how to refactor while maintaining clarity")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength",
    Justification = "Comparing to empty string communicates intention more clearly")]
public static void
Increment(VerbotRepository repository, bool major, bool minor)
{
    var patch = !(major || minor);

    // Check for no uncommitted changes
    if (repository.HasUncommittedChanges())
        throw new UserException("Uncommitted changes in repository");

    // Get current version
    var version = GetCommand.Get(repository);
    var minorVersion = version.Change(null, null, 0, "", "");
    var minorVersionString = FormattableString.Invariant($"{version.Major}.{version.Minor}");
    var patchVersionString = FormattableString.Invariant($"{version.Major}.{version.Minor}.{version.Patch}");

    // Check for release or -master prerelease
    if (!(version.Prerelease == "" || version.Prerelease == "master"))
        throw new UserException(
            "Expected current version to be a release or -master prerelease, but found " + version.ToString());
        
    // Get branch information
    var currentBranch = repository.GetBranch();
    var currentIsMaster = (currentBranch == "master");
    var masterBranches = FindMasterBranches(repository);

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
            repository.CreateBranch(newBranch);
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
            repository.CreateBranch(newBranch);
            repository.Checkout(newBranch);
        }
    }

    // Advance to new version
    Trace.TraceInformation("Incrementing to version " + newVersion.ToString() + " on branch " + repository.GetBranch());
    SetCommand.Set(repository, newVersion);
    repository.StageChanges();
    repository.Commit(FormattableString.Invariant($"Increment to version {newVersion}"));
}


/// <summary>
/// Find all 'master' and 'MAJOR.MINOR-master' branches and the MAJOR.MINOR versions they track, in decreasing version
/// order
/// </summary>
///
static IList<MasterBranch>
FindMasterBranches(VerbotRepository repository)
{
    var results = new List<MasterBranch>();

    var branches = repository.GetBranches();
    var currentBranch = repository.GetBranch();
    
    var masterBranch = branches.Where(n => n == "master").FirstOrDefault();
    if (masterBranch != null)
    {
        SemVersion masterVersion = null;
        try
        {
            repository.Checkout(masterBranch);
            masterVersion = GetCommand.Get(repository);
        }
        finally
        {
            repository.Checkout(currentBranch);
        }
        var masterMinorVersion = masterVersion.Change(null, null, 0, "", "");
        results.Add(new MasterBranch(masterBranch, masterMinorVersion));
    }

    results.AddRange(
        branches
            .Select(name => new {
                Name = name,
                Match = Regex.Match(name, @"^(\d+)\.(\d+)-master$") })
            .Where(m => m.Match.Success)
            .Select(m =>
                new MasterBranch(
                    m.Name,
                    new SemVersion(
                        int.Parse(m.Match.Groups[1].Value, CultureInfo.InvariantCulture),
                        int.Parse(m.Match.Groups[2].Value, CultureInfo.InvariantCulture)))));

    results.Sort((x,y) => y.Version.CompareTo(x.Version));

    return results;
}


class
MasterBranch
{
    public
    MasterBranch(GitCommitName name, SemVersion version)
    {
        Name = name;
        Version = version;
    }

    public GitCommitName
    Name
    {
        get;
    }

    public SemVersion
    Version
    {
        get;
    }

    public override string ToString()
    {
        return FormattableString.Invariant($"{Name} -> {Version.Major}.{Version.Minor}");
    }
}


}
}
