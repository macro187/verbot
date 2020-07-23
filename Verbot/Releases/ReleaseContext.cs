using System.Collections.Generic;
using System.Linq;
using MacroSemver;
using Verbot.Commits;
using Verbot.Refs;

namespace Verbot.Releases
{
    class ReleaseContext
    {

        readonly RefContext RefContext;


        public ReleaseContext(RefContext refContext)
        {
            RefContext = refContext;
        }


        IEnumerable<ReleaseInfo> ReleasesAscendingCache;
        IEnumerable<ReleaseInfo> ReleasesDescendingCache;
        IDictionary<SemVersion, ReleaseInfo> VersionReleaseLookupCache;
        ILookup<CommitInfo, ReleaseInfo> CommitReleaseLookupCache;


        public IEnumerable<ReleaseInfo> ReleasesAscending =>
            ReleasesAscendingCache ?? (ReleasesAscendingCache =
                RefContext.ReleaseTags
                    .Select(tag => new ReleaseInfo(this, tag))
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


        public IEnumerable<ReleaseInfo> PatchReleases =>
            ReleasesDescending.Where(r => r.IsPatch);


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


        public ILookup<CommitInfo, ReleaseInfo> CommitReleaseLookup =>
            CommitReleaseLookupCache ?? (CommitReleaseLookupCache =
                ReleasesDescending.ToLookup(t => t.Commit));


        IDictionary<SemVersion, ReleaseInfo> VersionReleaseLookup =>
            VersionReleaseLookupCache ?? (VersionReleaseLookupCache =
                ReleasesDescending.ToDictionary(t => t.Version));


        public ReleaseInfo GetRelease(SemVersion version) =>
            VersionReleaseLookup[version];


        public ReleaseInfo FindRelease(SemVersion version) =>
            VersionReleaseLookup.ContainsKey(version) ? VersionReleaseLookup[version] : null;


        public IEnumerable<ReleaseInfo> GetReleases(CommitInfo commit) =>
            CommitReleaseLookup.Contains(commit)
                ? CommitReleaseLookup[commit]
                : Enumerable.Empty<ReleaseInfo>();


        public IEnumerable<GitRefWithRemote> FindReleaseTagsWithRemote() =>
            RefContext.GetRemoteInfo(ReleasesDescending.Select(t => t.Tag)).ToList();

    }
}
