using System;
using MacroGuards;

namespace Verbot.Checks
{
    class CheckFailure
    {

        public static CheckFailure Fail(string description, string repairDescription)
        {
            return new CheckFailure(description, repairDescription);
        }


        CheckFailure(string description, string repairDescription, Action repair = null)
        {
            Guard.NotNull(description, nameof(description));
            Guard.NotNull(repairDescription, nameof(repairDescription));
            Description = description;
            RepairDescription = repairDescription;
            Repair = repair;
        }


        public string Description { get; }
        public string RepairDescription { get; }
        public Action Repair { get; }

    }
}
