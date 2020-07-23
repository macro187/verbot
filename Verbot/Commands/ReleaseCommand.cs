using System.Collections.Generic;
using System.Diagnostics;
using MacroExceptions;
using MacroGit;

namespace Verbot.Commands
{
    class ReleaseCommand : ICommand
    {

        readonly Context Context;


        public ReleaseCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");
            Release();
            return 0;
        }


        void Release()
        {
            // CheckMasterBranches();  // So master branch hasn't gone past feature/breaking commits

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

            var version = Context.CalculationContext.Calculate(Context.RefContext.Head.Target).CalculatedReleaseVersion;

            if (Context.ReleaseContext.FindRelease(version) != null)
            {
                throw new UserException($"Version {version} has already been released");
            }

            Trace.TraceInformation($"Tagging {version}");
            Context.GitRepository.CreateTag(new GitRefNameComponent(version));
        }

    }
}
