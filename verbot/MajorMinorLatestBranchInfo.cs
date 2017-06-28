using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;
using Semver;


namespace
verbot
{


internal class
MajorMinorLatestBranchInfo
{


public static bool
IsMajorMinorLatestBranchName(string name)
{
    Guard.NotNull(name, nameof(name));
    if (Regex.IsMatch(name, Pattern)) return true;
    return false;
}


const string Pattern = @"^(\d+)\.(\d+)-latest$";


public
MajorMinorLatestBranchInfo(VerbotRepository repository, GitCommitName name)
{
    Guard.NotNull(repository, nameof(repository));
    var match = Regex.Match(name, Pattern);
    if (!match.Success) throw new ArgumentException("Not a MAJOR.MINOR-latest branch name", "name");
    Repository = repository;
    Name = name;
    Version = new SemVersion(
        int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
        int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
}


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
    Justification = "Keep for consistency")]
public VerbotRepository
Repository
{
    get;
}


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
    Justification = "Keep for consistency")]
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


}
}
