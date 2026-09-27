using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Autocompletion;
using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Registry;
using NUnit.Framework;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class AutocompleteProviderTests
    {
        private AutocompleteProvider provider;

        [SetUp]
        public void SetUp()
        {
            var registry = new CommandRegistry();
            registry.Register(new SpyCommand(new CommandDescriptor("theme", "",
                parameters: new[] { ParameterSpec.Optional("name", ArgumentTypes.Choice("theme", () => new[] { "Dark", "Light", "Solarized Dark" })) })));
            registry.Register(new SpyCommand(new CommandDescriptor("history", "",
                flags: new[] { FlagSpec.WithValue("limit", ArgumentTypes.PositiveInt), FlagSpec.Switch("clear") })));
            registry.Register(new SpyCommand(new CommandDescriptor("repeat", "",
                parameters: new[] { ParameterSpec.Required("count", ArgumentTypes.PositiveInt), ParameterSpec.Remainder("command") })));
            registry.Register(new SpyCommand(new CommandDescriptor("clear", "", aliases: new[] { "cls" })));
            provider = new AutocompleteProvider(registry);
        }

        [Test]
        public void FirstToken_SuggestsCommandsAndAliases()
        {
            Assert.That(provider.Suggest("cl"), Is.EqualTo(new[] { "cls", "clear" }));
            Assert.That(provider.Suggest("th"), Is.EqualTo(new[] { "theme" }));
        }

        [Test]
        public void ArgumentValues_ComeFromTheArgumentType()
        {
            Assert.That(provider.Suggest("theme d"), Is.EqualTo(new[] { "Dark" }));
            Assert.That(provider.Suggest("theme ").Count(), Is.EqualTo(3));
        }

        [Test]
        public void Flags_AreSuggestedAfterADash()
        {
            Assert.That(provider.Suggest("history --l"), Is.EqualTo(new[] { "--limit=" }));
            Assert.That(provider.Suggest("history -").Count(), Is.EqualTo(2));
        }

        [Test]
        public void Remainder_SuggestsCommandNames()
        {
            Assert.That(provider.Suggest("repeat 3 hi"), Is.EqualTo(new[] { "history" }));
        }

        [Test]
        public void NoSuggestions_ForUnknownCommandsOrBlankInput()
        {
            Assert.That(provider.Suggest("nope x"), Is.Empty);
            Assert.That(provider.Suggest(""), Is.Empty);
            Assert.That(provider.Suggest("clear x"), Is.Empty);
        }
    }
}
