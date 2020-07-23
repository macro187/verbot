using MacroGuards;

namespace Verbot.Checks
{
    class CheckFailure
    {

        public static CheckFailure Fail(string description, string repairDescription)
        {
            return new CheckFailure(description, repairDescription);
        }


        CheckFailure(string description, string repairDescription)
        {
            Guard.NotNull(description, nameof(description));
            Guard.NotNull(repairDescription, nameof(repairDescription));
            Description = description;
            RepairDescription = repairDescription;
        }


        public string Description { get; }
        public string RepairDescription { get; }

    }
}
