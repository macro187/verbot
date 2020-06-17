using MacroSemver;

namespace Verbot
{
    partial class VerbotRepository
    {

        public SemVersion CalculateVersion() =>
            CalculateVersion(Head.Target);


        public SemVersion CalculateReleaseVersion() =>
            CalculateReleaseVersion(Head.Target);


        public SemVersion CalculatePrereleaseVersion() =>
            CalculatePrereleaseVersion(Head.Target);


        public SemVersion CalculateVersion(CommitInfo commit) =>
            GetCommitState(commit).Version;


        public SemVersion CalculatePrereleaseVersion(CommitInfo commit) =>
            GetCommitState(commit).CalculatedPrereleaseVersion;


        public SemVersion CalculateReleaseVersion(CommitInfo commit) =>
            GetCommitState(commit).ReleaseVersion ?? GetCommitState(commit).CalculatedReleaseVersion;

    }
}
