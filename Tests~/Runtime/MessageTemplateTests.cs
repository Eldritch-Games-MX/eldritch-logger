using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Sinks.Files;
using EldritchGames.EldritchLogger.Sinks.Network;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace EldritchGames.EldritchLogger.Tests
{
    public class MessageTemplateTests
    {
        private static (string message, Dictionary<string, object> properties) Render(string template, params object[] args)
        {
            var properties = new Dictionary<string, object>();
            var message = MessageTemplate.Parse(template).Render(args, properties);
            return (message, properties);
        }

        [Test]
        public void FillsHolesInOrder_AndCapturesValues()
        {
            var (message, properties) = Render("Player {Name} took {Damage} damage", "Bob", 12);

            Assert.That(message, Is.EqualTo("Player Bob took 12 damage"));
            Assert.That(properties["Name"], Is.EqualTo("Bob"));
            Assert.That(properties["Damage"], Is.EqualTo(12), "the original value is kept, not its text");
        }

        [Test]
        public void FormatsUseInvariantCulture()
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                Assert.That(Render("{Ratio:0.00} {Value}", 0.5, 1.25).message, Is.EqualTo("0.50 1.25"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void EscapesMissingAndExtraArguments()
        {
            Assert.That(Render("{{literal}} {A}", 1).message, Is.EqualTo("{literal} 1"));
            Assert.That(Render("{A} and {B}", 1).message, Is.EqualTo("1 and {B}"));
            Assert.That(Render("{A}", 1, 2, 3).message, Is.EqualTo("1"));
            Assert.That(Render("{A}", new object[] { null }).message, Is.EqualTo("null"));
        }

        [Test]
        public void InvalidHolesAreLiteral_AndRepeatedNamesAreKept()
        {
            Assert.That(Render("json {\"a\": 1} {}", 5).message, Is.EqualTo("json {\"a\": 1} {}"));
            Assert.That(Render("unclosed {A", 5).message, Is.EqualTo("unclosed {A"));

            var (message, properties) = Render("{X} vs {X}", 1, 2);
            Assert.That(message, Is.EqualTo("1 vs 2"));
            Assert.That(properties["X"], Is.EqualTo(1));
            Assert.That(properties["X_2"], Is.EqualTo(2));
        }

        [Test]
        public void ParseIsCached()
        {
            Assert.That(MessageTemplate.Parse("cached {A}"), Is.SameAs(MessageTemplate.Parse("cached {A}")));
            Assert.That(MessageTemplate.Parse("cached {A}").PropertyNames, Is.EqualTo(new[] { "A" }));
        }

        // 9 ----------------------------------------------------------------------------------------------

        [Test]
        public void TemplateCache_StaysBounded_AndKeepsWorking()
        {
            for (int i = 0; i < 2500; i++)
                MessageTemplate.Parse($"one-off template {i} {{Value}}");

            Assert.That(MessageTemplate.CacheCount, Is.LessThanOrEqualTo(1000));
            var properties = new System.Collections.Generic.Dictionary<string, object>();
            Assert.That(MessageTemplate.Parse("still {Works}").Render(new object[] { "fine" }, properties), Is.EqualTo("still fine"));
            Assert.That(MessageTemplate.Parse("still {Works}"), Is.SameAs(MessageTemplate.Parse("still {Works}")));
        }
    }
}
