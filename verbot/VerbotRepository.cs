using System;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path;
using MacroExceptions;
using MacroGit;
using MacroSln;


namespace
verbot
{


    public class
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


public IList<string>
FindAssemblyInfos()
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


}
}
