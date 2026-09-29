using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Commands;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class CommandArgumentsTests
    {
        private static CommandArguments Bind(CommandDescriptor descriptor, string input) =>
            new ArgumentBinder().Bind(descriptor, ConsoleTestHelpers.Parse(input)).Arguments;

        [Test]
        public void Accessors_FallBackToDefaults()
        {
            var descriptor = new CommandDescriptor("cmd", "",
                parameters: new[] { ParameterSpec.Optional("n", ArgumentTypes.Int) },
                flags: new[] { FlagSpec.WithValue("tag", ArgumentTypes.String) });

            var args = Bind(descriptor, "cmd");

            Assert.That(args.Has("n"), Is.False);
            Assert.That(args.GetOrDefault("n", 42), Is.EqualTo(42));
            Assert.That(args.GetFlag("tag", "none"), Is.EqualTo("none"));
            Assert.That(args.GetAll<int>("n"), Is.Empty);
            Assert.That(args.GetRemainder("n"), Is.Empty);
            Assert.Throws<KeyNotFoundException>(() => args.Get<int>("n"));
        }

        [Test]
        public void GetFlag_WithTheWrongType_ReturnsTheDefault()
        {
            var descriptor = new CommandDescriptor("cmd", "", flags: new[] { FlagSpec.WithValue("tag", ArgumentTypes.String) });

            var args = Bind(descriptor, "cmd --tag=abc");

            Assert.That(args.GetFlag("--tag", 0), Is.EqualTo(0), "stored as string, asked as int");
            Assert.That(args.GetFlag<string>("--tag"), Is.EqualTo("abc"), "leading dashes are ignored");
        }

        [Test]
        public void Descriptor_UsageMarksRequiredFlags_AndFindsFlagsCaseInsensitively()
        {
            var descriptor = new CommandDescriptor("cmd", "",
                parameters: new[] { ParameterSpec.Variadic("ids", ArgumentTypes.Int, required: true) },
                flags: new[] { FlagSpec.WithValue("tag", ArgumentTypes.String, required: true), FlagSpec.Switch("--quiet") });

            Assert.That(descriptor.Usage, Is.EqualTo("cmd <ids:int...> --tag=<string> [--quiet]"));
            Assert.That(descriptor.FindFlag("TAG"), Is.Not.Null);
            Assert.That(descriptor.FindFlag("quiet").Name, Is.EqualTo("quiet"));
        }
    }
}
