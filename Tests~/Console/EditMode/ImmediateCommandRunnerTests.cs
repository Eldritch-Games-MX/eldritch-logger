using EldritchGames.EldritchLogger.Console.Execution;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ImmediateCommandRunnerTests
    {
        private static IEnumerator Nested(List<string> log)
        {
            log.Add("outer start");
            yield return Inner(log);
            log.Add("outer end");
        }

        private static IEnumerator Inner(List<string> log)
        {
            log.Add("inner");
            yield return null;
        }

        private static IEnumerator Forever()
        {
            while (true) yield return null;
        }

        private static IEnumerator Throws()
        {
            yield return null;
            throw new InvalidOperationException("boom");
        }

        [Test]
        public void ImmediateRunner_RunsNestedEnumerators()
        {
            var log = new List<string>();
            new ImmediateCommandRunner().Run(Nested(log), _ => Assert.Fail("no error expected"));
            Assert.That(log, Is.EqualTo(new[] { "outer start", "inner", "outer end" }));
        }

        [Test]
        public void ImmediateRunner_ReportsExceptions_AndRunawayRoutines()
        {
            Exception thrown = null, runaway = null;
            new ImmediateCommandRunner().Run(Throws(), e => thrown = e);
            new ImmediateCommandRunner().Run(Forever(), e => runaway = e);

            Assert.That(thrown, Is.InstanceOf<InvalidOperationException>().And.Message.EqualTo("boom"));
            Assert.That(runaway, Is.InstanceOf<InvalidOperationException>().And.Message.Contains("did not finish"));
        }
    }
}
