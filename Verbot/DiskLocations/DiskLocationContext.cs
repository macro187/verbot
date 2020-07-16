using System.Collections.Generic;
using System.Linq;
using MacroExceptions;
using MacroGit;
using MacroGuards;
using MacroSemver;
using Verbot.DiskLocations.DotNet;

namespace Verbot.DiskLocations
{
    class DiskLocationContext
    {

        readonly GitRepository GitRepository;


        public DiskLocationContext(GitRepository gitRepository)
        {
            GitRepository = gitRepository;
        }


        public SemVersion ReadVersion()
        {
            var locations = FindOnDiskLocations();

            var version =
                locations
                    .Select(l => l.Read())
                    .Where(v => v != null)
                    .Distinct()
                    .SingleOrDefault();

            if (version == null)
            {
                throw new UserException("No version recorded on disk");
            }

            return version;
        }


        public void WriteVersion(SemVersion version)
        {
            Guard.NotNull(version, nameof(version));

            foreach (var location in FindOnDiskLocations())
            {
                location.Write(version);
            }
        }


        public IReadOnlyCollection<IDiskLocation> FindOnDiskLocations() =>
            new DotNetDiskLocationContext(GitRepository).FindDiskLocations();

    }
}
