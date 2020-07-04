using System.Linq;
using System.Collections.Generic;
using MacroSln;
using System;

namespace Verbot
{
    partial class VerbotRepository
    {

        IReadOnlyCollection<IOnDiskLocation> FindDotNetOnDiskLocations() =>
            FindProjects()
                .Select(p => new DotNetOnDiskLocation(p))
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
