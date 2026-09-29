using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Sinks.Files;
using EldritchGames.EldritchLogger.Sinks.Network;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests
{
    public class SelfLogTests
    {
        [TearDown]
        public void TearDown()
        {
            SelfLog.MaxReports = 100;
            SelfLog.Output = null;
            SelfLog.Reset();
        }

        [Test]
        public void Reports_AreCapped_AndResetRestoresTheBudget()
        {
            var messages = new List<string>();
            SelfLog.Output = messages.Add;
            SelfLog.MaxReports = 2;
            SelfLog.Reset();

            for (int i = 0; i < 5; i++) SelfLog.Report("r" + i);
            Assert.That(messages, Is.EqualTo(new[] { "[EldritchLogger] r0", "[EldritchLogger] r1" }));

            SelfLog.Reset();
            SelfLog.Report("after reset", new InvalidOperationException("why"));
            Assert.That(messages.Last(), Is.EqualTo("[EldritchLogger] after reset: InvalidOperationException: why"));
        }

        [Test]
        public void ReportingFromInsideAReport_IsIgnored_AndIsReportingIsScoped()
        {
            var messages = new List<string>();
            bool sawReporting = false;
            SelfLog.Reset();
            SelfLog.Output = m =>
            {
                sawReporting = SelfLog.IsReporting;
                messages.Add(m);
                SelfLog.Report("nested"); // must not recurse
            };

            SelfLog.Report("outer");

            Assert.That(messages, Is.EqualTo(new[] { "[EldritchLogger] outer" }));
            Assert.That(sawReporting, Is.True);
            Assert.That(SelfLog.IsReporting, Is.False);
        }

        [Test]
        public void ThrowingOutput_IsSwallowed()
        {
            SelfLog.Reset();
            SelfLog.Output = _ => throw new InvalidOperationException("output broken");
            Assert.DoesNotThrow(() => SelfLog.Report("x"));
        }
    }
}
