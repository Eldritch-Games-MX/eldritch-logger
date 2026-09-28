using EldritchLogger.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

// Minimal test runner: `dotnet run` exits with 1 if any case fails.
internal static class Program
{
    // Stand-ins for the Unity and logger types the analyzer recognises by full name.
    private const string Stubs = @"
namespace UnityEngine
{
    public static class Debug { public static void Log(object m) { } public static void LogWarning(object m) { } }
    public class Object { }
    public class Component : Object { }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }
}
namespace EldritchGames.EldritchLogger.Core
{
    public enum LogLevel { Debug, Info, Warning, Error, Critical }
    public struct LogCategory { public static readonly LogCategory General = default; }
    public interface IEldritchLogger { bool IsEnabled(LogLevel level, LogCategory category); }
    public static class ELoggerFactory
    {
        public static IEldritchLogger GetLogger<T>() => null;
        public static IEldritchLogger GetLogger(string name) => null;
    }
    public static class EldritchLoggerExtensions
    {
        public static void Log(this IEldritchLogger l, LogLevel level, LogCategory category, string message) { }
        public static EldritchGames.EldritchLogger.Builder.ILogBuilder At(this IEldritchLogger l, LogLevel level, LogCategory c = default) => null;
        public static EldritchGames.EldritchLogger.Builder.ILogBuilder AtDebug(this IEldritchLogger l, LogCategory c = default) => null;
        public static EldritchGames.EldritchLogger.Builder.ILogBuilder AtInfo(this IEldritchLogger l, LogCategory c = default) => null;
    }
}
namespace EldritchGames.EldritchLogger.Builder
{
    public interface ILogBuilder { ILogBuilder AddKeyValue(string k, object v); void Log(string message); }
}
";

    private static readonly List<string> Failures = new();

    private static int Main()
    {
        Case("ELG001: Debug.Log in game code", Game("UnityEngine.Debug.Log(\"hi\");"), "ELG001");
        Case("ELG001: Debug.LogWarning", Game("UnityEngine.Debug.LogWarning(\"hi\");"), "ELG001");
        CaseIn("Assembly-CSharp-Editor", "ELG001: not reported in editor assemblies", Game("UnityEngine.Debug.Log(\"hi\");"));
        CaseIn("MyGame.Editor.Tools", "ELG001: not reported in *.Editor.* assemblies", Game("UnityEngine.Debug.Log(\"hi\");"));
        CaseIn("MyEditorGame", "ELG001: \"Editor\" inside a word still counts as game code", Game("UnityEngine.Debug.Log(\"hi\");"), "ELG001");
        Case("ELG001: not reported inside the logger package",
             "namespace EldritchGames.EldritchLogger.Sinks { class S { void M() { UnityEngine.Debug.Log(1); } } }");

        Case("ELG002: interpolated AtDebug message", Game("logger.AtDebug().Log($\"hp {hp}\");"), "ELG002");
        Case("ELG002: through a builder chain", Game("logger.AtDebug().AddKeyValue(\"k\", 1).Log(\"hp \" + hp);"), "ELG002");
        Case("ELG002: At(LogLevel.Debug)", Game("logger.At(LogLevel.Debug).Log(string.Format(\"{0}\", hp));"), "ELG002");
        Case("ELG002: direct Log at Debug", Game("logger.Log(LogLevel.Debug, LogCategory.General, $\"hp {hp}\");"), "ELG002");
        Case("ELG002: guarded by IsEnabled",
             Game("if (logger.IsEnabled(LogLevel.Debug, LogCategory.General)) logger.AtDebug().Log($\"hp {hp}\");"));
        Case("ELG002: constant message", Game("logger.AtDebug().Log(\"constant\");"));
        Case("ELG002: interpolation without holes", Game("logger.AtDebug().Log($\"constant\");"));
        Case("ELG002: Info level is not reported", Game("logger.AtInfo().Log($\"hp {hp}\");"));
        Case("ELG002: else branch is not guarded",
             Game("if (logger.IsEnabled(LogLevel.Debug, LogCategory.General)) { } else logger.AtDebug().Log($\"hp {hp}\");"), "ELG002");

        Case("ELG003: MonoBehaviour field initializer", @"
using EldritchGames.EldritchLogger.Core;
class Player : UnityEngine.MonoBehaviour { IEldritchLogger logger = ELoggerFactory.GetLogger<Player>(); }", "ELG003");
        Case("ELG003: property initializer", @"
using EldritchGames.EldritchLogger.Core;
class Player : UnityEngine.MonoBehaviour { IEldritchLogger Logger { get; } = ELoggerFactory.GetLogger(""P""); }", "ELG003");
        Case("ELG003: assignment in Awake is fine", @"
using EldritchGames.EldritchLogger.Core;
class Player : UnityEngine.MonoBehaviour { IEldritchLogger logger; void Awake() { logger = ELoggerFactory.GetLogger<Player>(); } }");
        Case("ELG003: plain classes are fine", @"
using EldritchGames.EldritchLogger.Core;
class Service { IEldritchLogger logger = ELoggerFactory.GetLogger<Service>(); }");

        Console.WriteLine(Failures.Count == 0 ? "All analyzer cases passed." : $"{Failures.Count} case(s) failed.");
        return Failures.Count == 0 ? 0 : 1;
    }

    private static string Game(string body) => $@"
using EldritchGames.EldritchLogger.Core;
namespace Game
{{
    class Player
    {{
        IEldritchLogger logger;
        int hp;
        void Update() {{ {body} }}
    }}
}}";

    private static void Case(string name, string source, params string[] expectedIds) =>
        CaseIn("Test", name, source, expectedIds);

    private static void CaseIn(string assemblyName, string name, string source, params string[] expectedIds)
    {
        var actual = Analyze(source, assemblyName).Select(d => d.Id).OrderBy(id => id).ToArray();
        var expected = expectedIds.OrderBy(id => id).ToArray();
        bool ok = actual.SequenceEqual(expected);
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}  [{string.Join(",", actual)}]");
        if (!ok) Failures.Add(name);
    }

    private static ImmutableArray<Diagnostic> Analyze(string source, string assemblyName)
    {
        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
        var references = trustedAssemblies.Select(p => MetadataReference.CreateFromFile(p));

        var compilation = CSharpCompilation.Create(assemblyName,
            new[] { CSharpSyntaxTree.ParseText(Stubs), CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
            throw new InvalidOperationException("Test source does not compile: " + string.Join("; ", errors.Select(e => e.ToString())));

        return compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EldritchLoggerAnalyzer()))
                          .GetAnalyzerDiagnosticsAsync().Result;
    }
}
