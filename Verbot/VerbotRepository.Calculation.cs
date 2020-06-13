using MacroSemver;

namespace Verbot
{
    partial class VerbotRepository
    {

        public SemVersion CalculateVersion() =>
            CalculateVersion(GetHeadCommit());


        public SemVersion CalculateReleaseVersion() =>
            CalculateReleaseVersion(GetHeadCommit());


        public SemVersion CalculatePrereleaseVersion() =>
            CalculatePrereleaseVersion(GetHeadCommit());


        public SemVersion CalculateVersion(CommitInfo commit) =>
            GetCommitState(commit).Version;


        public SemVersion CalculatePrereleaseVersion(CommitInfo commit) =>
            GetCommitState(commit).CalculatedPrereleaseVersion;


        public SemVersion CalculateReleaseVersion(CommitInfo commit) =>
            GetCommitState(commit).ReleaseVersion ?? GetCommitState(commit).CalculatedReleaseVersion;

    }
}
