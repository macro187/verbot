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

        public SemVersion WriteVersion(bool verbose)
        {
            var version = CalculateVersion(verbose);
            WriteToVersionLocations(version);
            return version;
        }


        public SemVersion WriteReleaseVersion(bool verbose)
        {
            var version = CalculateReleaseVersion(verbose);
            WriteToVersionLocations(version);
            return version;
        }


        public SemVersion WritePrereleaseVersion(bool verbose)
        {
            var version = CalculatePrereleaseVersion(verbose);
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
