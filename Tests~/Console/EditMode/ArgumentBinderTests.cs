using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Commands;
using NUnit.Framework;
using System;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ArgumentBinderTests
    {
        private enum Mode { Fast, Slow }

        private static readonly CommandDescriptor Repeat = new("repeat", "",
            parameters: new[] { ParameterSpec.Required("count", ArgumentTypes.PositiveInt), ParameterSpec.Remainder("command") },
            flags: new[] { FlagSpec.Switch("silent"), FlagSpec.WithValue("delay", ArgumentTypes.NonNegativeInt) });

        private static readonly CommandDescriptor Typed = new("typed", "",
            parameters: new[]
            {
                ParameterSpec.Required("n", ArgumentTypes.Int),
                ParameterSpec.Optional("mode", ArgumentTypes.Enum<Mode>()),
                ParameterSpec.Variadic("rest", ArgumentTypes.Float)
            },
            flags: new[] { FlagSpec.WithValue("tag", ArgumentTypes.String, required: true) });

        private static BindResult Bind(CommandDescriptor descriptor, string input) =>
            new ArgumentBinder().Bind(descriptor, ConsoleTestHelpers.Parse(input));

        [Test]
        public void BindsTypedPositionalsVariadicsAndFlags()
        {
            var result = Bind(Typed, "typed -3 slow 1.5 2 --tag=x");

            Assert.That(result.Success, Is.True, result.Error);
            var args = result.Arguments;
            Assert.That(args.Get<int>("n"), Is.EqualTo(-3));
            Assert.That(args.Get<Mode>("mode"), Is.EqualTo(Mode.Slow));
            Assert.That(args.GetAll<float>("rest"), Is.EqualTo(new[] { 1.5f, 2f }));
            Assert.That(args.GetFlag<string>("tag"), Is.EqualTo("x"));
        }

        [Test]
        public void FlagValue_CanBeTheNextToken()
        {
            var history = new CommandDescriptor("history", "", flags: new[] { FlagSpec.WithValue("limit", ArgumentTypes.PositiveInt) });

            var result = Bind(history, "history --limit 2");

            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(result.Arguments.GetFlag("limit", 0), Is.EqualTo(2));
        }

        [Test]
        public void RequiredFlag_IsDetected()
        {
            Assert.That(Bind(Typed, "typed 1 --tag y").Success, Is.True);

            var missing = Bind(Typed, "typed 1");
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Error, Does.Contain("--tag").And.Contain("Usage: typed"));
        }

        [Test]
        public void RepeatFlags_BeforeTheCommand_BelongToRepeat()
        {
            var result = Bind(Repeat, "repeat 3 --silent --delay=10 echo hi --loud");

            Assert.That(result.Success, Is.True, result.Error);
            var args = result.Arguments;
            Assert.That(args.Get<int>("count"), Is.EqualTo(3));
            Assert.That(args.HasFlag("silent"), Is.True);
            Assert.That(args.GetFlag("delay", 0), Is.EqualTo(10));
            Assert.That(args.GetRemainder("command").Select(t => t.ToSourceText()), Is.EqualTo(new[] { "echo", "hi", "--loud" }));
        }

        [Test]
        public void FlagsAfterTheRemainderStart_BelongToTheInnerCommand()
        {
            var result = Bind(Repeat, "repeat 3 echo --silent");

            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(result.Arguments.HasFlag("silent"), Is.False);
            Assert.That(result.Arguments.GetRemainder("command").Select(t => t.ToSourceText()), Is.EqualTo(new[] { "echo", "--silent" }));
        }

        [TestCase("repeat 0 echo", "must be positive")]
        [TestCase("repeat x echo", "not a whole number")]
        [TestCase("repeat 2", "Missing required argument <command>")]
        [TestCase("repeat 2 --nope echo", "Unknown flag '--nope'")]
        [TestCase("repeat 2 --silent=yes echo", "does not take a value")]
        [TestCase("repeat 2 --delay", "requires a value")]
        [TestCase("typed 1 2 3 --tag t extra", "not a valid Mode")]
        public void InvalidInput_ReportsAHelpfulError(string input, string expected)
        {
            var descriptor = input.StartsWith("typed") ? Typed : Repeat;

            var result = Bind(descriptor, input);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain(expected));
        }

        [Test]
        public void TooManyArguments_AreRejected()
        {
            var clear = new CommandDescriptor("clear", "");
            Assert.That(Bind(clear, "clear now").Error, Does.StartWith("Too many arguments"));
        }

        [Test]
        public void Descriptor_ValidatesParameterOrder()
        {
            Assert.Throws<ArgumentException>(() => new CommandDescriptor("bad", "",
                parameters: new[] { ParameterSpec.Optional("a", ArgumentTypes.Int), ParameterSpec.Required("b", ArgumentTypes.Int) }));
            Assert.Throws<ArgumentException>(() => new CommandDescriptor("bad", "",
                parameters: new[] { ParameterSpec.Remainder("a"), ParameterSpec.Required("b", ArgumentTypes.Int) }));
            Assert.Throws<ArgumentException>(() => new CommandDescriptor("two words", ""));
        }

        [Test]
        public void Usage_DescribesParametersAndFlags()
        {
            Assert.That(Repeat.Usage, Is.EqualTo("repeat <count:int+> <command...> [--silent] [--delay=<int>]"));
        }
    }
}
