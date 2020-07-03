using System.Collections.Generic;
using System.Linq;
using MacroSemver;

namespace Verbot
{
    partial class VerbotRepository
    {

        IDictionary<CommitInfo, CommitState> CommitStateCache = new Dictionary<CommitInfo, CommitState>();


        public IEnumerable<CommitState> CommitStates =>
            CommitStateCache.Values;


        public SemVersion CalculateVersion() =>
            GetVersion(Head.Target);


        public SemVersion CalculateReleaseVersion() =>
            GetReleaseVersion(Head.Target);


        public SemVersion CalculatePrereleaseVersion() =>
            GetPrereleaseVersion(Head.Target);


        public SemVersion GetVersion(CommitInfo commit) =>
            GetCommitState(commit).Version;


        public SemVersion GetPrereleaseVersion(CommitInfo commit) =>
            GetCommitState(commit).CalculatedPrereleaseVersion;


        public SemVersion GetReleaseVersion(CommitInfo commit) =>
            GetCommitState(commit).ReleaseVersion ?? GetCommitState(commit).CalculatedReleaseVersion;


        public CommitState GetCommitState(CommitInfo commit) =>
            CommitStateCache.TryGetValue(commit, out var state)
                ? state
                : CalculateCommitStatesTo(commit);


        public CommitState CalculateCommitStatesTo(CommitInfo to) =>
            to.GetCommitsSince(null)
                .Aggregate(
                    new CommitState()
                    {
                        Minor = 1,
                    },
                    (previousState, commit) => 
                        CommitStateCache.ContainsKey(commit)
                            ? CommitStateCache[commit]
                            : CommitStateCache[commit] = CalculateCommitState(commit, previousState));


        CommitState CalculateCommitState(CommitInfo commit, CommitState previousState)
        {
            var state = new CommitState
            {
                Commit =
                    commit,
                ReleaseTag =
                    GetReleaseTags(commit).SingleOrDefault(), // Error if multiple
                IsFeature =
                    commit.IsFeature,
                IsFirstFeatureSincePreviousRelease =
                    commit.IsFeature && !previousState.HasBeenFeatureSincePreviousRelease,
                HasBeenFeatureSincePreviousRelease =
                    commit.IsFeature || previousState.HasBeenFeatureSincePreviousRelease,
                IsBreaking =
                    commit.IsBreaking,
                IsFirstBreakingSincePreviousRelease =
                    commit.IsBreaking && !previousState.HasBeenBreakingSincePreviousRelease,
                HasBeenBreakingSincePreviousRelease =
                    commit.IsBreaking || previousState.HasBeenBreakingSincePreviousRelease,
                CommitsSincePreviousRelease =
                    previousState.CommitsSincePreviousRelease + 1,
                CommitterDatePrereleaseComponent =
                    commit.CommitDate
                        .ToUniversalTime()
                        .ToString("yyyyMMddTHHmmss"),
                ShortSha1PrereleaseComponent =
                    commit.Sha1.ToString().Substring(0, 4),
                Major =
                    previousState.Major,
                Minor =
                    previousState.Minor,
                Patch =
                    previousState.Patch,
            };

            //
            // Advance after a release
            //
            if (previousState.ReleaseVersion != null)
            {
                state.Patch++;
                state.CommitsSincePreviousRelease = 1;
            }

            //
            // Advance on feature / breaking change
            //
            if (state.IsFirstBreakingSincePreviousRelease)
            {
                state.Major++;
                state.Minor = 0;
                state.Patch = 0;
            }
            else if (state.IsFirstFeatureSincePreviousRelease && !state.HasBeenBreakingSincePreviousRelease)
            {
                state.Minor++;
                state.Patch = 0;
            }

            //
            // Assemble prerelease version component
            //
            state.Prerelease =
                string.Concat(
                    "alpha",
                    ".", state.CommitsSincePreviousRelease,
                    ".", state.CommitterDatePrereleaseComponent,
                    ".", state.ShortSha1PrereleaseComponent);

            //
            // Assemble calculated versions
            //
            state.CalculatedPrereleaseVersion =
                new SemVersion(state.Major, state.Minor, state.Patch, state.Prerelease, state.Build);
            state.CalculatedReleaseVersion =
                new SemVersion(state.Major, state.Minor, state.Patch);

            //
            // Factor in release tag if present
            //
            if (state.ReleaseTag != null)
            {
                state.ReleaseVersion = state.ReleaseTag.Version;
                state.Major = state.ReleaseVersion.Major;
                state.Minor = state.ReleaseVersion.Minor;
                state.Patch = state.ReleaseVersion.Patch;
                state.Prerelease = "";
            }

            //
            // Final version
            //
            state.Version = state.ReleaseVersion ?? state.CalculatedPrereleaseVersion;

            return state;
        }

    }
}
