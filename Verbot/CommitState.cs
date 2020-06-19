using MacroSemver;

namespace Verbot
{
    class CommitState
    {

        public CommitInfo Commit { get; set; }
        public ReleaseTagInfo ReleaseTag { get; set; }
        public bool IsFeature { get; set; }
        public bool IsFirstFeatureSincePreviousRelease { get; set; }
        public bool HasBeenFeatureSincePreviousRelease { get; set; }
        public bool IsBreaking { get; set; }
        public bool IsFirstBreakingSincePreviousRelease { get; set; }
        public bool HasBeenBreakingSincePreviousRelease { get; set; }
        public int CommitsSincePreviousRelease { get; set; }
        public string CommitterDatePrereleaseComponent { get; set; } = "";
        public string ShortSha1PrereleaseComponent { get; set; }
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public string Prerelease { get; set; } = "";
        public string Build { get; set; } = "";
        public SemVersion CalculatedPrereleaseVersion { get; set; }
        public SemVersion CalculatedReleaseVersion { get; set; }
        public SemVersion ReleaseVersion { get; set; }
        public SemVersion Version { get; set; }
        public SemVersion ReleaseSeries => Version?.Change(patch: 0, prerelease: "", build: "");

    }
}
