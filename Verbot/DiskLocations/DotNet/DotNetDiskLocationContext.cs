using System.Linq;
using System.Collections.Generic;
using MacroSln;
using System;
using MacroGit;

namespace Verbot.DiskLocations.DotNet
{
    class DotNetDiskLocationContext
    {

        readonly VisualStudioSolution Solution;


        public DotNetDiskLocationContext(GitRepository gitRepository)
        {
            Solution = VisualStudioSolution.Find(gitRepository.Path);
        }


        public IReadOnlyCollection<IDiskLocation> FindDiskLocations() =>
            FindProjects()
                .Select(p => new DotNetDiskLocation(p))
                .ToList();


        ICollection<VisualStudioProject> FindProjects() =>
            // All projects
            Solution.ProjectReferences
                // ...that are C# projects
                .Where(pr =>
                    pr.TypeId == VisualStudioProjectTypeIds.CSharp ||
                    pr.TypeId == VisualStudioProjectTypeIds.CSharpNew)
                // ..."local" to the solution
                .Where(pr => !pr.Location.StartsWith("..", StringComparison.Ordinal))
                .Select(pr => pr.GetProject())
                .ToList();

    }
}
