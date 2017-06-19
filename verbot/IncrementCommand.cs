using System;
using System.Diagnostics;
using MacroExceptions;
using MacroGit;

namespace
verbot
{


internal static class
IncrementCommand
{


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Globalization",
    "CA1303:Do not pass literals as localized parameters",
    MessageId = "MacroGit.GitRepository.Commit(System.String)",
    Justification = "Only english is supported")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Performance",
    "CA1820:TestForEmptyStringsUsingStringLength",
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
    var minorVersion = FormattableString.Invariant($"{version.Major}.{version.Minor}");
    var patchVersion = FormattableString.Invariant($"{version.Major}.{version.Minor}.{version.Patch}");

    // Get current branches
    var branches = repository.GetBranches();

    // Check for release or -master prerelease
    if (!(version.Prerelease == "" || version.Prerelease == "master"))
        throw new UserException(
            "Expected current version to be a release or -master prerelease, but found " + version.ToString());
        
    // Determine correct branch (MAJOR.MINOR-master)
    var correctBranch = FormattableString.Invariant($"{version.Major}.{version.Minor}-master");

    // Check that we're on correct branch
    var currentBranch = repository.GetBranch();
    if (currentBranch != correctBranch)
        throw new UserException("Expected to be on branch " + correctBranch);

    if (version.Prerelease == "master")
    {
        // Check that !(-master && patch) no need to skip unreleased patch version
        if (patch)
            throw new UserException(FormattableString.Invariant(
                $"No need to increment patch when {patchVersion} hasn't been released yet"));

        // Check that !(-master && minor && PATCH == 0) no need to skip unreleased minor version
        if (minor && version.Patch == 0)
            throw new UserException(FormattableString.Invariant(
                $"No need to increment minor when {patchVersion} hasn't been released yet"));
            
        // Check that !(-master && major && MINOR == 0 && PATCH == 0) no need to skip unreleased major version
        if (major && version.Minor == 0 && version.Patch == 0)
            throw new UserException(FormattableString.Invariant(
                $"No need to increment major when {minorVersion} hasn't been released yet"));
    }

    // Compute new version: Increment major, minor, or patch; zero out minor and/or patch; -master prerelease tag
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

    // If incrementing major or minor
    if (major || minor)
    {
        var newBranch = new GitCommitName(FormattableString.Invariant($"{newVersion.Major}.{newVersion.Minor}-master"));

        // Check for no existing NEWMAJOR.NEWMINOR-master branch
        if (branches.Contains(newBranch))
            throw new UserException("A " + newBranch + " branch already exists");

        // git checkout -b NEWMAJOR.NEWMINOR-master
        Trace.TraceInformation("Switching to new branch " + newBranch);
        repository.CreateBranch(newBranch);
        repository.Checkout(newBranch);
    }

    // Set new version
    Trace.TraceInformation("Incrementing to version " + newVersion.ToString());
    SetCommand.Set(repository, newVersion);
    repository.StageChanges();
    repository.Commit("Increment to version " + newVersion.ToString());
}


}
}
