namespace Verbot
{
    partial class VerbotCommitInfo
    {

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (!(obj is VerbotCommitInfo commit)) return false;
            return Equals(commit);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                int hash = typeof(VerbotCommitInfo).GetHashCode();
                hash = hash * 23 + Sha1.GetHashCode();
                return hash;
            }
        }

    }
}
