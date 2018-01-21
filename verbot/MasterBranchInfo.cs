using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;
using MacroSemver;


namespace
verbot
{


internal class
MasterBranchInfo
{


public static bool
IsMasterBranchName(string name)
{
    Guard.NotNull(name, nameof(name));
    if (name == "master") return true;
    if (Regex.IsMatch(name, MasterBranchPattern)) return true;
    return false;
}


const string MasterBranchPattern = @"^(\d+)\.(\d+)-master$";


public
MasterBranchInfo(VerbotRepository repository, GitCommitName name)
{
    Guard.NotNull(repository, nameof(repository));
    if (!IsMasterBranchName(name)) throw new ArgumentException("Not a master branch name", "name");
    Repository = repository;
    Name = name;
    Version = FindVersion();
}


public VerbotRepository
Repository
{
    get;
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


SemVersion FindVersion()
{
    SemVersion version;
    if (Name == "master")
    {
        var currentBranch = Repository.GetBranch();
        try
        {
            Repository.Checkout(Name);
            version = Repository.GetVersion();
        }
        finally
        {
            Repository.Checkout(currentBranch);
        }
        version = version.Change(null, null, 0, "", "");
    }
    else
    {
        var match = Regex.Match(Name, MasterBranchPattern);
        version = new SemVersion(
            int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
    }
    return version;
}


}
}
