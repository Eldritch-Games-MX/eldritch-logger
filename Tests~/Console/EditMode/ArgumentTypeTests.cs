using EldritchGames.EldritchLogger.Console.Arguments;
using NUnit.Framework;
using System;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ArgumentTypeTests
    {
        private enum Speed { Slow, Fast }

        [Test]
        public void Float_ParsesInvariantCulture()
        {
            Assert.That(ArgumentTypes.Float.TryParse("1.5", out var value, out _), Is.True);
            Assert.That(value, Is.EqualTo(1.5f));
            Assert.That(ArgumentTypes.Float.TryParse("abc", out _, out var error), Is.False);
            Assert.That(error, Does.Contain("not a number"));
        }

        [Test]
        public void Bool_ParsesCommonSpellings_AndSuggests()
        {
            Assert.That(ArgumentTypes.Bool.Suggest("t"), Is.EqualTo(new[] { "true" }));
            foreach (var yes in new[] { "TRUE", "on", "Yes", "1" })
            {
                Assert.That(ArgumentTypes.Bool.TryParse(yes, out var value, out _), Is.True, yes);
                Assert.That(value, Is.EqualTo(true));
            }
            Assert.That(ArgumentTypes.Bool.TryParse("off", out var off, out _), Is.True);
            Assert.That(off, Is.EqualTo(false));
            Assert.That(ArgumentTypes.Bool.TryParse("maybe", out _, out var error), Is.False);
            Assert.That(error, Does.Contain("not a bool"));
        }

        [Test]
        public void Double_ParsesInvariantCulture()
        {
            Assert.That(ArgumentTypes.Double.TryParse("2.25", out var value, out _), Is.True);
            Assert.That(value, Is.EqualTo(2.25));
            Assert.That(ArgumentTypes.Double.TryParse("x", out _, out _), Is.False);
        }

        [Test]
        public void Enum_ParsesNames_AndSuggests()
        {
            var type = ArgumentTypes.Enum<Speed>();
            Assert.That(type.Name, Is.EqualTo("Speed"));
            Assert.That(type.Suggest("f"), Is.EqualTo(new[] { "Fast" }));
            Assert.That(type.TryParse("slow", out var value, out _), Is.True);
            Assert.That(value, Is.EqualTo(Speed.Slow));
        }

        [Test]
        public void IntTypes_EnforceTheirMinimum()
        {
            Assert.That(ArgumentTypes.NonNegativeInt.TryParse("0", out _, out _), Is.True);
            Assert.That(ArgumentTypes.NonNegativeInt.TryParse("-1", out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at least 0"));
            Assert.That(ArgumentTypes.Int.TryParse("-5", out var value, out _), Is.True);
            Assert.That(value, Is.EqualTo(-5));
            Assert.That(ArgumentTypes.String.Suggest("x"), Is.Empty);
            Assert.That(ArgumentTypes.Int.Suggest("1"), Is.Empty);
        }

        [Test]
        public void FloatAndDouble_RejectNaNAndInfinity()
        {
            foreach (var type in new[] { ArgumentTypes.Float, ArgumentTypes.Double })
                foreach (var raw in new[] { "NaN", "Infinity", "-Infinity", "1e400" })
                    Assert.That(type.TryParse(raw, out _, out _), Is.False, $"{type.Name} {raw}");
        }
    }
}
