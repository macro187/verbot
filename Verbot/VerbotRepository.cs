using MacroExceptions;
using MacroGit;
using MacroGuards;
using MacroSemver;
using MacroSln;

namespace Verbot
{
    partial class VerbotRepository
    {

        static readonly SemVersion DefaultVersion = new SemVersion(9999, 0, 0, "alpha");


        readonly GitRepository GitRepository;
        readonly VisualStudioSolution Solution;


        public VerbotRepository(GitRepository gitRepository)
        {
            Guard.NotNull(gitRepository, nameof(gitRepository));

            var solution = VisualStudioSolution.Find(gitRepository.Path);
            if (solution == null)
            {
                throw new UserException("No Visual Studio solution found in repository");
            }

            GitRepository = gitRepository;
            Solution = solution;
        }

    }
}
