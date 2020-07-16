using System.Diagnostics;
using MacroGit;
using MacroGuards;
using MacroSemver;
using Verbot.Calculations;
using Verbot.Checks;
using Verbot.Commits;
using Verbot.DiskLocations;
using Verbot.LatestBranches;
using Verbot.MasterBranches;
using Verbot.Refs;
using Verbot.Releases;

namespace Verbot
{
    partial class Context
    {

        public static readonly SemVersion DefaultVersion = new SemVersion(9999, 0, 0, "alpha");
        

        readonly bool Verbose;


        public Context(GitRepository gitRepository, bool verbose)
        {
            Guard.NotNull(gitRepository, nameof(gitRepository));
            Verbose = verbose;
            GitRepository = gitRepository;
            BuildContexts();
        }


        public GitRepository GitRepository { get; }
        public DiskLocationContext DiskLocationContext { get; private set; }
        public CommitContext CommitContext { get; private set; }
        public RefContext RefContext { get; private set; }
        public CalculationContext CalculationContext { get; private set; }
        public ReleaseContext ReleaseContext { get; private set; }
        public LatestBranchContext LatestBranchContext { get; private set; }
        public MasterBranchContext MasterBranchContext { get; private set; }
        public CheckContext CheckContext { get; private set; }


        public void TraceVerbose(string message)
        {
            if (!Verbose) return;
            Trace.TraceInformation(message);
        }


        void BuildContexts()
        {
            DiskLocationContext = new DiskLocationContext(GitRepository);
            CommitContext = new CommitContext(GitRepository);
            RefContext = new RefContext(GitRepository, CommitContext);
            CalculationContext = new CalculationContext(RefContext);
            ReleaseContext = new ReleaseContext(RefContext, CalculationContext, GitRepository);
            LatestBranchContext = new LatestBranchContext(ReleaseContext);
            MasterBranchContext = new MasterBranchContext(ReleaseContext, RefContext, CalculationContext);
            CheckContext = new CheckContext(
                MasterBranchContext, LatestBranchContext, ReleaseContext, RefContext, CalculationContext);
        }

    }
}
