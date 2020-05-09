using MacroGuards;
using MacroSemver;
using MacroSln;

namespace Verbot
{

    /// <summary>
    /// A location where the current version can be recorded
    /// </summary>
    ///
    class VersionLocation
    {

        public VersionLocation(VisualStudioProject project)
        {
            Guard.NotNull(project, nameof(project));
            Project = project;
        }


        public VisualStudioProject Project { get; }


        /// <summary>
        /// A description of the location
        /// </summary>
        ///
        public string Description
        {
            get
            {
                return Project.Path;
            }
        }


        /// <summary>
        /// Get the version recorded at the location
        /// </summary>
        ///
        /// <returns>
        /// The version recorded at the location, or <c>null</c> if no version is recorded there
        /// </returns>
        ///
        public SemVersion GetVersion()
        {
            var versionString = Project.GetProperty("Version");
            return
                !string.IsNullOrWhiteSpace(versionString) ?
                    SemVersion.Parse(versionString) :
                    null;
        }


        /// <summary>
        /// Set the version recorded at the location
        /// </summary>
        ///
        public void SetVersion(SemVersion version)
        {
            Guard.NotNull(version, nameof(version));

            var assemblyVersion = $"{version.Major}.0.0.0";
            var assemblyFileVersion = $"{version.Major}.{version.Minor}.{version.Patch}.0";

            Project.SetProperty("Version", version.ToString());
            Project.SetProperty("AssemblyFileVersion", assemblyFileVersion);
            Project.SetProperty("AssemblyVersion", assemblyVersion);
            Project.Save();
        }

    }
}
