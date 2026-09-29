using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Commands.BuiltIn;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.UI;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.EldritchLogger.Console.Tests.PlayMode
{
    public class CoroutineCommandRunnerTests
    {
        private sealed class Host : MonoBehaviour { }

        private sealed class ListView : IConsoleOutputView
        {
            public readonly List<string> Lines = new();
            public void AppendLog(string message) => Lines.Add(message);
            public void Clear() => Lines.Clear();
        }

        private sealed class Counter : IConsoleCommand
        {
            public int Count;
            public CommandDescriptor Descriptor { get; } = new("count", "");
            public void Execute(CommandContext context) => Count++;
        }

        private GameObject hostObject;

        [SetUp]
        public void SetUp() => hostObject = new GameObject("Host", typeof(Host));

        [TearDown]
        public void TearDown() => UnityEngine.Object.Destroy(hostObject);

        [UnityTest]
        public IEnumerator RepeatWithDelay_DoesNotBlockTheMainThread()
        {
            var registry = new CommandRegistry();
            var counter = new Counter();
            registry.Register(counter);
            var view = new ListView();
            var executor = new CommandExecutor(registry, new Lexer(), new CommandParser(), new ArgumentBinder(),
                new CommandHistory(10), new ConsoleOutput(view), new CoroutineCommandRunner(hostObject.GetComponent<Host>()));
            registry.Register(new RepeatCommand(executor));

            float start = Time.realtimeSinceStartup;
            var status = executor.Execute("repeat 3 --delay=100 count");
            float elapsedSync = Time.realtimeSinceStartup - start;

            Assert.That(status, Is.EqualTo(ExecutionStatus.Started));
            Assert.That(elapsedSync, Is.LessThan(0.05f), "Execute returns immediately");
            Assert.That(counter.Count, Is.EqualTo(1));

            yield return new WaitForSecondsRealtime(0.5f);

            Assert.That(counter.Count, Is.EqualTo(3));
            Assert.That(view.Lines[view.Lines.Count - 1], Does.StartWith("Finished repeating"));
        }

        [UnityTest]
        public IEnumerator ExceptionsInsideCoroutines_AreReported()
        {
            Exception reported = null;
            new CoroutineCommandRunner(hostObject.GetComponent<Host>()).Run(Failing(), ex => reported = ex);

            yield return null;
            yield return null;

            Assert.That(reported, Is.InstanceOf<InvalidOperationException>());
        }

        private static IEnumerator Failing()
        {
            yield return null;
            throw new InvalidOperationException("late failure");
        }
    }
}
