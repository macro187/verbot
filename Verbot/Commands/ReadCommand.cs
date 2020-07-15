using System.Linq;
using MacroExceptions;
using MacroSemver;

namespace Verbot
{
    class ReadCommand
    {

        readonly Context Context;


        public ReadCommand(Context context)
        {
            Context = context;
        }


        public SemVersion ReadVersion()
        {
            Context.CheckContext.CheckLocal();

            var locations = Context.DiskLocationContext.FindOnDiskLocations();

            var version =
                locations
                    .Select(l => l.Read())
                    .Where(v => v != null)
                    .Distinct()
                    .SingleOrDefault();

            if (version == null)
            {
                throw new UserException("No version recorded on disk");
            }

            return version;
        }

    }
}
