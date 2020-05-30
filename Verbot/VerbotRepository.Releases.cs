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
        ILookup<VerbotCommitInfo, ReleaseInfo> CommitReleaseLookupCache;


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
                    .Select(tag => new ReleaseInfo(this, tag.Version, GetCommit(tag.Ref.Target), tag.Ref))
                    .OrderBy(release => release.Version)
                    .ToList());


        public IEnumerable<ReleaseInfo> ReleasesDescending =>
            ReleasesDescendingCache ?? (ReleasesDescendingCache =
                ReleasesAscending.Reverse().ToList());


        public IEnumerable<ReleaseInfo> MajorReleases =>
            ReleasesDescending.Where(r => r.IsMajor);

        
        public IEnumerable<ReleaseInfo> MinorReleases =>
            ReleasesDescending.Where(r => r.IsMinor);

        
        IDictionary<SemVersion, ReleaseInfo> VersionReleaseLookup =>
            VersionReleaseLookupCache ?? (VersionReleaseLookupCache =
                ReleasesDescending.ToDictionary(t => t.Version));


        public ReleaseInfo GetRelease(SemVersion version) =>
            VersionReleaseLookup[version];


        public ReleaseInfo FindRelease(SemVersion version) =>
            VersionReleaseLookup.ContainsKey(version) ? VersionReleaseLookup[version] : null;


        ILookup<VerbotCommitInfo, ReleaseInfo> CommitReleaseLookup =>
            CommitReleaseLookupCache ?? (CommitReleaseLookupCache =
                ReleasesDescending.ToLookup(t => t.Commit));


        public IEnumerable<ReleaseInfo> GetReleases(VerbotCommitInfo commit) =>
            CommitReleaseLookup.Contains(commit)
                ? CommitReleaseLookup[commit]
                : Enumerable.Empty<ReleaseInfo>();


        public IEnumerable<GitRefWithRemote> FindReleaseTagsWithRemote() =>
            GetRemoteInfo(ReleasesDescending.Select(t => t.Tag)).ToList();

    }
}
