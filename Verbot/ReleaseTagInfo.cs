using System;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    class ReleaseTagInfo
    {

        public ReleaseTagInfo(SemVersion version, RefInfo @ref)
        {
            Guard.NotNull(version, nameof(version));
            if (version.Prerelease != "" || version.Build != "")
            {
                throw new ArgumentException("Not a release version", nameof(version));
            }
            Guard.NotNull(@ref, nameof(@ref));
            if (!@ref.IsTag)
            {
                throw new ArgumentException("Not a tag", nameof(@ref));
            }

            Version = version;
            Ref = @ref;
        }


        public SemVersion Version { get; }
        public RefInfo Ref { get; }

    }
}
