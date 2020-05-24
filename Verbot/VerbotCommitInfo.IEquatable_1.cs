using System;

namespace Verbot
{
    partial class VerbotCommitInfo : IEquatable<VerbotCommitInfo>
    {

        public static bool operator ==(VerbotCommitInfo a, VerbotCommitInfo b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }


        public static bool operator !=(VerbotCommitInfo a, VerbotCommitInfo b)
        {
            return !(a == b);
        }


        public bool Equals(VerbotCommitInfo commit)
        {
            if (commit is null) return false;
            return commit.Sha1 == Sha1;
        }

    }
}
