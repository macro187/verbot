using System.Collections.Generic;
using MacroGit;

namespace Verbot
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
