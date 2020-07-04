using System.Linq;
using MacroSemver;
using MacroExceptions;
using MacroGuards;
using System.Collections.Generic;

namespace Verbot
{
    partial class VerbotRepository
    {

        public SemVersion WriteVersion() =>
            WriteVersion(Calculate(Head.Target).Version);

          
        public SemVersion WriteReleaseVersion() =>
            WriteVersion(Calculate(Head.Target).CalculatedReleaseVersion);


        public SemVersion WritePrereleaseVersion() =>
            WriteVersion(Calculate(Head.Target).CalculatedPrereleaseVersion);


        public SemVersion WriteDefaultVersion() =>
            WriteVersion(DefaultVersion);


        public SemVersion ReadVersion()
        {
            CheckLocal();

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


        SemVersion WriteVersion(SemVersion version)
        {
            Guard.NotNull(version, nameof(version));

            CheckForVersionLocationsOnDisk();

            foreach (var location in FindOnDiskLocations())
            {
                location.Write(version);
            }

            return version;
        }


        IReadOnlyCollection<IOnDiskLocation> FindOnDiskLocations() =>
            FindDotNetOnDiskLocations();

    }
}
