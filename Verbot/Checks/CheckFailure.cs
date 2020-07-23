using MacroGuards;

namespace Verbot.Checks
{
    class CheckFailure
    {

        public static CheckFailure Fail(string description)
        {
            return new CheckFailure(description);
        }


        CheckFailure(string description)
        {
            Guard.NotNull(description, nameof(description));
            Description = description;
        }


        public string Description { get; }

    }
}
