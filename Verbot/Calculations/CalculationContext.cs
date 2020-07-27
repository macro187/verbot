using System.Collections.Generic;
using System.Linq;
using MacroSemver;
using Verbot.Commits;
using Verbot.Refs;

namespace Verbot.Calculations
{
    class CalculationContext
    {

        readonly RefContext RefContext;
        readonly IDictionary<CommitInfo, CommitState> CommitStateCache = new Dictionary<CommitInfo, CommitState>();


        public CalculationContext(RefContext refContext)
        {
            RefContext = refContext;
        }


        public CommitState Calculate(CommitInfo commit) =>
            CommitStateCache.TryGetValue(commit, out var state)
                ? state
                : CalculateTo(commit);


        public CommitState CalculateTo(CommitInfo to) =>
            to.GetCommitsSince(null)
                .Aggregate(
                    new CommitState()
                    {
                        Minor = 1,
                    },
                    (previousState, commit) =>
                        CommitStateCache.ContainsKey(commit)
                            ? CommitStateCache[commit]
                            : CommitStateCache[commit] = Calculate(commit, previousState));


        CommitState Calculate(CommitInfo commit, CommitState previousState)
        {
            var state = new CommitState
            {
                Commit =
                    commit,
                ReleaseTag =
                    RefContext.GetReleaseTags(commit).SingleOrDefault(), // Error if multiple
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
            if (previousState.TaggedReleaseVersion != null)
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
                state.TaggedReleaseVersion = state.ReleaseTag.Version;
                state.Major = state.TaggedReleaseVersion.Major;
                state.Minor = state.TaggedReleaseVersion.Minor;
                state.Patch = state.TaggedReleaseVersion.Patch;
                state.Prerelease = "";
            }

            //
            // Final version
            //
            state.Version = state.TaggedReleaseVersion ?? state.CalculatedPrereleaseVersion;

            return state;
        }

    }
}
