using EldritchGames.EldritchLogger.Core;
using NUnit.Framework;
using System;

namespace EldritchGames.EldritchLogger.Tests
{
    public class NullLoggerTests
    {
        [Test]
        public void NullLogger_IsDisabled_AndIgnoresEverything()
        {
            var logger = NullLogger.Instance;

            Assert.That(logger.IsEnabled(LogLevel.Critical, LogCategory.General), Is.False);
            Assert.DoesNotThrow(() => logger.Log(null));
            Assert.DoesNotThrow(() => logger.Error(new Exception("x"), "t {A}", 1));

            var builder = logger.AtCritical(LogCategory.AI);
            Assert.That(builder.Category(LogCategory.UI), Is.SameAs(builder));
            Assert.That(builder.AddKeyValue("k", 1), Is.SameAs(builder));
            Assert.That(builder.WithException(new Exception()), Is.SameAs(builder));
            Assert.That(builder.WithEvent(new object(), "e"), Is.SameAs(builder));
            Assert.That(builder.WithComponent(null), Is.SameAs(builder));
            Assert.That(builder.WithContext(null), Is.SameAs(builder));
            Assert.DoesNotThrow(() => builder.Log("m"));
            Assert.DoesNotThrow(() => builder.Log("m {A}", 1));
        }
    }
}
