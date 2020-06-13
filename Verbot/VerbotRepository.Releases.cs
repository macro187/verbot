using System.Collections.Generic;
using System.Linq;
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
            commit.CommitsSince(null)
                .Reverse()
                .Select(c => GetReleases(c).SingleOrDefault())
                .FirstOrDefault(r => r != null);

    }
}
