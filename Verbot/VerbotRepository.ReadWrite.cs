using System.Linq;
using MacroSemver;
using MacroExceptions;
using MacroGuards;
using System.Collections.Generic;
using MacroSln;
using System;

namespace Verbot
{
    partial class VerbotRepository
    {

        public SemVersion WriteVersion()
        {
            var version = CalculateVersion();
            WriteToVersionLocations(version);
            return version;
        }


        public SemVersion WriteReleaseVersion()
        {
            var version = CalculateReleaseVersion();
            WriteToVersionLocations(version);
            return version;
        }


        public SemVersion WritePrereleaseVersion()
        {
            var version = CalculatePrereleaseVersion();
            WriteToVersionLocations(version);
            return version;
        }


        public SemVersion ReadFromVersionLocations()
        {
            CheckLocal();

            var locations = FindVersionLocations();

            var version =
                locations
                    .Select(l => l.GetVersion())
                    .Where(v => v != null)
                    .Distinct()
                    .SingleOrDefault();

            if (version == null)
            {
                throw new UserException("No version recorded in repository");
            }

            return version;
        }


        public void WriteDefaultVersion()
        {
            WriteToVersionLocations(DefaultVersion);
        }


        void WriteToVersionLocations(SemVersion version)
        {
            Guard.NotNull(version, nameof(version));

            CheckForVersionLocations();

            foreach (var location in FindVersionLocations())
            {
                location.SetVersion(version);
            }
        }


        ICollection<VersionLocation> FindVersionLocations()
        {
            return
                FindProjects()
                    .Select(p => new VersionLocation(p))
                    .ToList();
        }


        ICollection<VisualStudioProject> FindProjects()
        {
            return
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
}
