using MacroSemver;

namespace Verbot
{

    /// <summary>
    /// An on-disk location where the current version can be recorded
    /// </summary>
    ///
    interface IOnDiskLocation
    {

        /// <summary>
        /// A natural-language description of the location
        /// </summary>
        ///
        string Description { get; }


        /// <summary>
        /// Read the version from the location
        /// </summary>
        ///
        /// <returns>
        /// The version recorded at the location, or <c>null</c> if no version is recorded there
        /// </returns>
        ///
        SemVersion Read();


        /// <summary>
        /// Write a version to the location
        /// </summary>
        ///
        void Write(SemVersion version);

    }
}
