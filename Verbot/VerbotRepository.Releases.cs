using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MacroExceptions;
using MacroGit;
using MacroSemver;

namespace Verbot
{
    partial class VerbotRepository
    {
        
        IEnumerable<ReleaseInfo> ReleasesAscendingCache;
        IEnumerable<ReleaseInfo> ReleasesDescendingCache;
        IDictionary<SemVersion, ReleaseInfo> VersionReleaseLookupCache;
        ILookup<CommitInfo, ReleaseInfo> CommitReleaseLookupCache;


        public IEnumerable<ReleaseInfo> ReleasesAscending =>
            ReleasesAscendingCache ?? (ReleasesAscendingCache =
                Tags
                    .Select(tag =>
                    {
                        SemVersion.TryParse(tag.Name, out var version, true);
                        return (Ref: tag, Version: version);
                    })
                    .Where(tag => tag.Version != null)
                    .Where(tag => tag.Version.Prerelease == "")
                    .Where(tag => tag.Version.Build == "")
                    .Select(tag => new ReleaseInfo(this, tag.Version, tag.Ref))
                    .OrderBy(release => release.Version)
                    .ToList());


        public IEnumerable<ReleaseInfo> ReleasesDescending =>
            ReleasesDescendingCache ?? (ReleasesDescendingCache =
                ReleasesAscending.Reverse().ToList());


        public ReleaseInfo LatestRelease =>
            ReleasesDescending.FirstOrDefault();


        public IEnumerable<ReleaseInfo> MajorReleases =>
            ReleasesDescending.Where(r => r.IsMajor);

        
        public IEnumerable<ReleaseInfo> MinorReleases =>
            ReleasesDescending.Where(r => r.IsMinor);


        public IEnumerable<ReleaseInfo> LatestMajorSeriesReleases =>
            ReleasesAscending
                .GroupBy(r => r.Version.Change(minor: 0, patch: 0))
                .Select(g => g.OrderBy(r => r.Version).Last())
                .OrderBy(r => r.Version);


        public IEnumerable<ReleaseInfo> LatestMinorSeriesReleases =>
            ReleasesAscending
                .GroupBy(r => r.Version.Change(patch: 0))
                .Select(g => g.OrderBy(r => r.Version).Last())
                .OrderBy(r => r.Version);
        
        IDictionary<SemVersion, ReleaseInfo> VersionReleaseLookup =>
            VersionReleaseLookupCache ?? (VersionReleaseLookupCache =
                ReleasesDescending.ToDictionary(t => t.Version));


        public void Release()
        {
            /*
            var commit = Head.Target;
            var state = GetCommitState(commit);

            // Calculate release version X.Y.Z
            var version = state.CalculatedReleaseVersion;

            // Check: No releases on this commit != X.Y.Z
            var areOtherReleasesOnCommit = GetReleases(commit).Any(r => r.Version != version);
            if (areOtherReleasesOnCommit)
            {
                throw new UserException("HEAD already released as a different version");
            }

            // Check: X.Y.Z hasn't been released (from a different commit, if on same commit warn?)
            var existingRelease = FindRelease(version);
            if (existingRelease != null)
            {
                throw new UserException($"{version} already released at {existingRelease.Commit.Sha1}");
            }

            // Check: On correct [X.Y-]master branch
            var masterBranchName = CalculateMasterBranchName(version);

            // (more checks?)

            // tag()
            GitRepository.CreateTag(new GitRefNameComponent(version));

            // if minor release
            //   create/move X-latest branch
            //   create/switchto [X.Y-]master branch
            //   create/move previous [-]master branch to most recent branch point. HARD!
            // create/move X.Y-latest branch
            // if latest release in repo
            //   create/move latest branch
            // if --push git push tag and updated branches
            // 

            // This is getting hard and duplicates some check/repair logic. Maybe just always do a full pre-check before
            // and repair after?  Wasn't the idea to be really strict anyways?  Yes but need repair command first.
            */


            /*
            // TODO Basic checks

            if (version != null)
            {
                Trace.TraceInformation($"Already released as {version}");
            }
            else
            {
                version = state.CalculatedReleaseVersion;
                var existingRelease = FindRelease(version);
                if (existingRelease != null)
                {
                    var existingSha1 = existingRelease.Commit.Sha1;
                    throw new UserException($"Can't release {version} because it already exists at {existingSha1}");
                }
            }
            */

            var version = Calculate(Head.Target).CalculatedReleaseVersion;

            if (FindRelease(version) != null)
            {
                throw new UserException($"Version {version} has already been released");
            }

            Trace.TraceInformation($"Tagging {version}");
            GitRepository.CreateTag(new GitRefNameComponent(version));
        }


        public ReleaseInfo GetRelease(SemVersion version) =>
            VersionReleaseLookup[version];


        public ReleaseInfo FindRelease(SemVersion version) =>
            VersionReleaseLookup.ContainsKey(version) ? VersionReleaseLookup[version] : null;


        ILookup<CommitInfo, ReleaseInfo> CommitReleaseLookup =>
            CommitReleaseLookupCache ?? (CommitReleaseLookupCache =
                ReleasesDescending.ToLookup(t => t.Commit));


        public IEnumerable<ReleaseInfo> GetReleases(CommitInfo commit) =>
            CommitReleaseLookup.Contains(commit)
                ? CommitReleaseLookup[commit]
                : Enumerable.Empty<ReleaseInfo>();


        public IEnumerable<GitRefWithRemote> FindReleaseTagsWithRemote() =>
            GetRemoteInfo(ReleasesDescending.Select(t => t.Tag)).ToList();


        public ReleaseInfo FindPreviousReleaseAncestor(CommitInfo commit) =>
            commit.GetCommitsSince(null)
                .Reverse()
                .Select(c => GetReleases(c).SingleOrDefault())
                .FirstOrDefault(r => r != null);

    }
}
