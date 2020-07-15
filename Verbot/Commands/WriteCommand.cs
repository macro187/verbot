using MacroSemver;
using MacroGuards;

namespace Verbot.Commands
{
    class WriteCommand
    {

        readonly Context Context;


        public WriteCommand(Context context)
        {
            Context = context;
        }


        public SemVersion WriteVersion() =>
            WriteVersion(Context.CalculationContext.Calculate(Context.RefContext.Head.Target).Version);


        public SemVersion WriteReleaseVersion() =>
            WriteVersion(Context.CalculationContext.Calculate(Context.RefContext.Head.Target).CalculatedReleaseVersion);


        public SemVersion WritePrereleaseVersion() =>
            WriteVersion(Context.CalculationContext.Calculate(Context.RefContext.Head.Target).CalculatedPrereleaseVersion);


        public SemVersion WriteDefaultVersion() =>
            WriteVersion(Context.DefaultVersion);


        SemVersion WriteVersion(SemVersion version)
        {
            Guard.NotNull(version, nameof(version));

            foreach (var location in Context.DiskLocationContext.FindOnDiskLocations())
            {
                location.Write(version);
            }

            return version;
        }

    }
}
