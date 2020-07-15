using MacroGuards;
using MacroSemver;
using MacroSln;

namespace Verbot.DiskLocations.DotNet
{

    class DotNetDiskLocation : IDiskLocation
    {

        readonly VisualStudioProject Project;


        public DotNetDiskLocation(VisualStudioProject project)
        {
            Guard.NotNull(project, nameof(project));
            Project = project;
        }


        public string Description => Project.Path;


        public SemVersion Read()
        {
            var versionString = Project.GetProperty("Version");
            if (string.IsNullOrWhiteSpace(versionString)) return null;
            return SemVersion.Parse(versionString);
        }


        public void Write(SemVersion version)
        {
            Guard.NotNull(version, nameof(version));

            var assemblyVersion = $"{version.Major}.0.0.0";
            var assemblyFileVersion = $"{version.Major}.{version.Minor}.{version.Patch}.0";

            Project.SetProperty("Version", version);
            Project.SetProperty("AssemblyFileVersion", assemblyFileVersion);
            Project.SetProperty("AssemblyVersion", assemblyVersion);
            Project.Save();
        }

    }
}
