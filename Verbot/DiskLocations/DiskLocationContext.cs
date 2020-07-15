using System.Collections.Generic;
using MacroGit;
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


        public IReadOnlyCollection<IDiskLocation> FindOnDiskLocations() =>
            new DotNetDiskLocationContext(GitRepository).FindDiskLocations();

    }
}
