using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Commands.BuiltIn;
using EldritchGames.EldritchLogger.Console.Commands.Reflection;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Services;
using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ReflectionCommandTests
    {
        public enum Speed { Slow, Fast }

        private static class Commands
        {
            [ConsoleMethod("add", "Adds two numbers.")]
            public static int Add(int a, int b = 10) => a + b;

            [ConsoleMethod] // default name: "greet"
            private static void Greet(string name, IConsoleOutput output) => output.Info($"hi {name}");

            [ConsoleMethod("sum")]
            public static float Sum(params float[] values) => values.Sum();

            [ConsoleMethod("mode")]
            public static string Mode(Speed speed, bool loud = false) => $"{speed}{(loud ? "!" : "")}";

            [ConsoleMethod("boom")]
            public static void Boom() => throw new InvalidOperationException("kaput");

            [ConsoleMethod("wait")]
            public static IEnumerator Wait(CommandContext context)
            {
                context.Output.Info("start");
                yield return null;
                context.Output.Info("end");
            }

            [ConsoleMethod("god", "Invulnerability.", IsCheat = true, Aliases = new[] { "iddqd" })]
            public static void God() => GodCalls++;

            [ConsoleMethod("bad")]
            public static void Bad(DateTime when) { }

            public static int GodCalls;
        }

        private static class Variables
        {
            [ConsoleVariable("speed", "Move speed.", Min = 0, Max = 10)]
            public static float MoveSpeed = 1;

            [ConsoleVariable] // default name: "godmode"
            public static bool GodMode;

            [ConsoleVariable("version")]
            public static string Version => "1.2";

            [ConsoleVariable("difficulty", IsCheat = true)]
            public static Speed Difficulty { get; set; }

            [ConsoleVariable("locked", ReadOnly = true)]
            public static int Locked = 3;

            [ConsoleVariable("broken")]
            public static Vector3 Broken;
        }

        private sealed class Pinger : MonoBehaviour
        {
            [ConsoleMethod("ping")]
            private string Ping() => "pong";
        }

        private CommandRegistry registry;
        private RecordingOutput output;
        private CommandExecutor executor;
        private DiscoveryReport report;
        private GameObject pinger;

        [SetUp]
        public void SetUp()
        {
            Commands.GodCalls = 0;
            Variables.MoveSpeed = 1;
            Variables.GodMode = false;
            Variables.Difficulty = Speed.Slow;

            registry = new CommandRegistry();
            output = new RecordingOutput();
            executor = ConsoleTestHelpers.CreateExecutor(registry, output);
            registry.Register(new HelpCommand(registry));
            registry.Register(new CvarsCommand(registry));

            var members = CommandDiscovery.FindCommandMembers(typeof(Commands))
                .Concat(CommandDiscovery.FindCommandMembers(typeof(Variables)))
                .Concat(CommandDiscovery.FindCommandMembers(typeof(Pinger)));
            report = new CommandDiscovery(new ConsoleServiceProvider()).RegisterAll(registry, Type.EmptyTypes, Type.EmptyTypes, members);
        }

        [TearDown]
        public void TearDown()
        {
            if (pinger != null) UnityEngine.Object.DestroyImmediate(pinger);
        }

        private string Run(string input)
        {
            output.Messages.Clear();
            executor.Execute(input);
            return string.Join("\n", output.Texts);
        }

        [Test]
        public void Methods_BindTypedArguments_AndPrintReturnValues()
        {
            Assert.That(Run("add 2"), Is.EqualTo("12"), "optional parameter uses its default");
            Assert.That(Run("add 2 3"), Is.EqualTo("5"));
            Assert.That(Run("sum 1 2.5 0.5"), Is.EqualTo("4"));
            Assert.That(Run("sum"), Is.EqualTo("0"), "params accepts nothing");
            Assert.That(Run("mode fast yes"), Is.EqualTo("Fast!"));
            Assert.That(Run("add x"), Does.Contain("not a whole number"));
        }

        [Test]
        public void Methods_GetOutputAndContextInjected_AndDefaultToTheLowerCaseName()
        {
            Assert.That(Run("greet Bob"), Is.EqualTo("hi Bob"));
            Assert.That(registry.TryGet("greet", out var greet), Is.True);
            Assert.That(greet.Descriptor.Usage, Is.EqualTo("greet <name:string>"), "injected parameters are not arguments");
        }

        [Test]
        public void Methods_AppearInHelpWithTheirUsage()
        {
            Assert.That(Run("help add"), Does.StartWith("add <a:int> [<b:int>] - Adds two numbers."));
            Assert.That(Run("help god"), Does.Contain("[cheat]").And.Contain("aliases: iddqd"));
        }

        [Test]
        public void Exceptions_AreReportedWithTheMethodsOwnException()
        {
            Assert.That(Run("boom"), Is.EqualTo("'boom' failed: InvalidOperationException: kaput"));
        }

        [Test]
        public void IEnumeratorMethods_RunAsCoroutines()
        {
            Assert.That(executor.Execute("wait"), Is.EqualTo(ExecutionStatus.Started));
            Assert.That(output.Texts, Is.EqualTo(new[] { "start", "end" }));
        }

        [Test]
        public void CheatMethods_FollowTheCheatPolicy_AndAliasesWork()
        {
            executor.CheatPolicy = FixedCheatPolicy.Deny;
            Assert.That(Run("iddqd"), Does.Contain("cheats are disabled"));

            executor.CheatPolicy = FixedCheatPolicy.Allow;
            Run("iddqd");
            Assert.That(Commands.GodCalls, Is.EqualTo(1));
        }

        [Test]
        public void UnsupportedMembers_AreSkippedWithAReason()
        {
            var reasons = report.SkippedTypes.Select(s => s.Reason).ToArray();
            Assert.That(reasons, Has.Some.Contains("Bad").And.Contains("DateTime"));
            Assert.That(reasons, Has.Some.Contains("Broken").And.Contains("Vector3"));
            Assert.That(registry.TryGet("bad", out _), Is.False);
        }

        [Test]
        public void InstanceMethods_RunOnEveryActiveMonoBehaviour()
        {
            Assert.That(Run("ping"), Does.Contain("No active Pinger"));

            pinger = new GameObject("Pinger A", typeof(Pinger));
            Assert.That(Run("ping"), Is.EqualTo("Pinger A: pong"));

            pinger.SetActive(false);
            Assert.That(Run("ping"), Does.Contain("No active Pinger"));
        }

        [Test]
        public void Variables_ReadAndWrite_WithRangeChecks()
        {
            Assert.That(Run("speed"), Is.EqualTo("speed = 1"));
            Assert.That(Run("speed 5.5"), Is.EqualTo("speed = 5.5"));
            Assert.That(Variables.MoveSpeed, Is.EqualTo(5.5f));

            Assert.That(Run("speed 11"), Does.Contain("between 0 and 10"));
            Assert.That(Variables.MoveSpeed, Is.EqualTo(5.5f), "rejected values are not applied");

            Assert.That(Run("godmode on"), Is.EqualTo("godmode = true"));
            Assert.That(Variables.GodMode, Is.True);
        }

        [Test]
        public void ReadOnlyVariables_CannotBeSet()
        {
            Assert.That(Run("version"), Is.EqualTo("version = 1.2"));
            Assert.That(Run("version 2"), Does.Contain("Too many arguments"));
            Assert.That(Run("locked 5"), Does.Contain("Too many arguments"));
            Assert.That(Variables.Locked, Is.EqualTo(3));
        }

        [Test]
        public void CheatVariables_CanBeReadButNotWrittenWithoutCheats()
        {
            executor.CheatPolicy = FixedCheatPolicy.Deny;
            Assert.That(Run("difficulty"), Is.EqualTo("difficulty = Slow"));
            Assert.That(Run("difficulty fast"), Does.Contain("cheats are disabled"));
            Assert.That(Variables.Difficulty, Is.EqualTo(Speed.Slow));

            executor.CheatPolicy = FixedCheatPolicy.Allow;
            Assert.That(Run("difficulty fast"), Is.EqualTo("difficulty = Fast"));
        }

        [Test]
        public void Cvars_ListsVariables_WithFlags_AndFilters()
        {
            var all = Run("cvars").Split('\n');
            Assert.That(all, Has.Some.EqualTo("speed = 1  (Move speed.)"));
            Assert.That(all, Has.Some.StartsWith("version = 1.2 [read-only]"));
            Assert.That(all, Has.Some.StartsWith("difficulty = Slow [cheat]"));

            Assert.That(Run("cvars spe"), Is.EqualTo("speed = 1  (Move speed.)"));
            Assert.That(Run("cvars nothing"), Is.EqualTo("No console variables matching 'nothing'."));
        }

        [Test]
        public void ArgumentTypeResolver_MapsSupportedTypes()
        {
            Assert.That(ArgumentTypeResolver.TryResolve(typeof(int), out var i) && i.Name == "int", Is.True);
            Assert.That(ArgumentTypeResolver.TryResolve(typeof(double), out var d) && d.Name == "double", Is.True);
            Assert.That(ArgumentTypeResolver.TryResolve(typeof(Speed), out var e) && e.Name == "Speed", Is.True);
            Assert.That(ArgumentTypeResolver.TryResolve(typeof(Vector3), out _), Is.False);
            Assert.That(ArgumentTypeResolver.TryResolve(null, out _), Is.False);
            Assert.That(ArgumentTypeResolver.Format(null), Is.EqualTo("null"));
            Assert.That(ArgumentTypeResolver.Format(1.5f), Is.EqualTo("1.5"));
        }

        /// <summary>A type whose members cannot be loaded, like one referencing a missing assembly.</summary>
        private sealed class UnloadableType : System.Reflection.TypeDelegator
        {
            public UnloadableType() : base(typeof(object)) { }

            public override System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr) =>
                throw new TypeLoadException("Could not load a referenced assembly");
        }

        [Test]
        public void BrokenTypes_AreSkippedByDiscovery_InsteadOfFailingIt()
        {
            Assert.Throws<TypeLoadException>(() => CommandDiscovery.FindCommandMembers(new UnloadableType()).ToList());
            Assert.That(CommandDiscovery.SafeFindCommandMembers(new UnloadableType()), Is.Empty);
            Assert.That(CommandDiscovery.SafeFindCommandMembers(typeof(Commands)).Count, Is.GreaterThan(0));
        }

        /// <summary>A method whose metadata cannot be read, like one using a type from a missing assembly.</summary>
        private sealed class UnreadableMethod : System.Reflection.MethodInfo
        {
            public override string Name => "Unreadable";
            public override Type DeclaringType => typeof(ReflectionCommandTests);
            public override Type ReflectedType => DeclaringType;
            public override System.Reflection.MethodAttributes Attributes => System.Reflection.MethodAttributes.Static;
            public override RuntimeMethodHandle MethodHandle => throw new TypeLoadException("missing assembly");
            public override System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes => throw new TypeLoadException("missing assembly");
            public override System.Reflection.MethodInfo GetBaseDefinition() => this;
            public override System.Reflection.ParameterInfo[] GetParameters() => throw new TypeLoadException("missing assembly");
            public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags() => default;
            public override object Invoke(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture) => throw new InvalidOperationException();
            public override object[] GetCustomAttributes(bool inherit) => throw new TypeLoadException("missing assembly");
            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new TypeLoadException("missing assembly");
            public override bool IsDefined(Type attributeType, bool inherit) => true;
        }

        [Test]
        public void UnexpectedReflectionErrors_SkipOnlyThatMember()
        {
            var registry = new CommandRegistry();
            var members = new System.Reflection.MemberInfo[] { new UnreadableMethod() }
                .Concat(CommandDiscovery.FindCommandMembers(typeof(Variables)));

            DiscoveryReport result = null;
            Assert.DoesNotThrow(() => result = new CommandDiscovery(new ConsoleServiceProvider()).RegisterAll(registry, Type.EmptyTypes, Type.EmptyTypes, members));

            Assert.That(result.SkippedTypes.Select(s => s.Reason), Has.Some.Contains("Unreadable").And.Contains("TypeLoadException"));
            Assert.That(registry.TryGet("speed", out _), Is.True, "members after the broken one are still registered");
        }

        [Test]
        public void DiscoveredMembers_ExcludeTestAssemblies()
        {
            Assert.That(CommandDiscovery.CommandMembers.Any(m => m.DeclaringType == typeof(Commands)), Is.False);
        }
    }
}
