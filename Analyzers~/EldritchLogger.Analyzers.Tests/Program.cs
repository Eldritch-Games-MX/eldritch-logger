using EldritchLogger.Analyzers;
using EldritchLogger.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;

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
        public static void Info(this IEldritchLogger l, string template, params object[] args) { }
        public static void Debug(this IEldritchLogger l, string template, params object[] args) { }
        public static void Error(this IEldritchLogger l, System.Exception exception, string template, params object[] args) { }
        public static void LogTemplate(this IEldritchLogger l, LogLevel level, LogCategory category, System.Exception exception, string template, params object[] args) { }
        public static System.IDisposable BeginScope(this IEldritchLogger l, string key, object value) => null;
    }
}
namespace EldritchGames.EldritchLogger.Pipeline
{
    public static class LogScope { public static System.IDisposable Push(string key, object value) => null; }
}
namespace EldritchGames.EldritchLogger.Builder
{
    public interface ILogBuilder { ILogBuilder AddKeyValue(string k, object v); void Log(string message); void Log(string template, params object[] args); }
}
";

    private static readonly List<string> Failures = new();

    private static int Main()
    {
        Case("ELG001: Debug.Log in game code", Game("UnityEngine.Debug.Log(\"hi\");"), "ELG001");
        Case("ELG001: Debug.LogWarning", Game("UnityEngine.Debug.LogWarning(\"hi\");"), "ELG001");
        Case("ELG004: interpolated template", Game("logger.Info($\"Player {hp} joined\");"), "ELG004");
        Case("ELG004: concatenated template with exception", Game("logger.Error(null, \"Save \" + hp + \" failed\");"), "ELG004");
        Case("ELG004: builder template overload", Game("logger.AtInfo().Log($\"hp {hp} of {{Max}}\", 100);"), "ELG004");
        Case("ELG004: named template argument", Game("logger.Info(args: new object[] { hp }, template: $\"hp {hp}\");"), "ELG004");
        Case("ELG004: proper template is fine", Game("logger.Info(\"Player {Name} joined\", hp);"));
        Case("ELG004: Debug shorthand is a template, not ELG002", Game("logger.Debug($\"hp {hp}\");"), "ELG004");
        Fix("ELG004 fix: interpolation becomes holes", new UseTemplateHolesCodeFix(), "ELG004",
            Game("logger.Info($\"Player {hp} joined\");"),
            Game("logger.Info(\"Player {Hp} joined\", hp);"));
        Fix("ELG004 fix: formats, member names and duplicates", new UseTemplateHolesCodeFix(), "ELG004",
            Game("logger.Info($\"{this.hp:0.0} and {hp}\\n\");"),
            Game("logger.Info(\"{Hp:0.0} and {Hp2}\\n\", this.hp, hp);"));
        Fix("ELG004 fix: concatenation, before an exception argument", new UseTemplateHolesCodeFix(), "ELG004",
            Game("logger.Error(null, \"Save \" + hp + \" failed\");"),
            Game("logger.Error(null, \"Save {Hp} failed\", hp);"));
        Fix("ELG004 fix: runtime holes in the text keep working", new UseTemplateHolesCodeFix(), "ELG004",
            Game("logger.Info($\"{{Name}} has {hp}\");"),
            Game("logger.Info(\"{Name} has {Hp}\", hp);"));
        Fix("ELG004 fix: not offered when existing arguments could fill text holes", new UseTemplateHolesCodeFix(), "ELG004",
            Game("logger.AtInfo().Log($\"hp {hp} of {{Max}}\", 100);"),
            null);
        Case("ELG005: using statement across yield",
             Coroutine("using (logger.BeginScope(\"MatchId\", 1)) { yield return null; }"), "ELG005");
        Case("ELG005: using var across yield",
             Coroutine("using var scope = logger.BeginScope(\"MatchId\", 1); yield return null;"), "ELG005");
        Case("ELG005: LogScope.Push",
             Coroutine("using (EldritchGames.EldritchLogger.Pipeline.LogScope.Push(\"A\", 1)) { yield return 1; }"), "ELG005");
        Case("ELG005: scope closed before yield",
             Coroutine("using (logger.BeginScope(\"MatchId\", 1)) { logger.Info(\"x\"); } yield return null;"));
        Case("ELG005: yield before using var",
             Coroutine("yield return null; using var scope = logger.BeginScope(\"MatchId\", 1);"));
        Case("ELG005: other disposables are fine",
             Coroutine("using (new System.IO.MemoryStream()) { yield return null; }"));
        Case("ELG005: a local iterator declared inside a scope is its own body",
             Coroutine("using (logger.BeginScope(\"A\", 1)) { System.Collections.IEnumerable Inner() { yield return 1; } } yield break;"));
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
        Case("ELG002: Debug template with a value-type argument", Game("logger.Debug(\"Hp {Hp}\", hp);"), "ELG002");
        Case("ELG002: AtDebug template with an argument", Game("logger.AtDebug().Log(\"Name {Name}\", \"bob\");"), "ELG002");
        Case("ELG002: LogTemplate at Debug", Game("logger.LogTemplate(LogLevel.Debug, LogCategory.General, null, \"Hp {Hp}\", hp);"), "ELG002");
        Case("ELG002: guarded Debug template",
             Game("if (logger.IsEnabled(LogLevel.Debug, LogCategory.General)) logger.Debug(\"Hp {Hp}\", hp);"));
        Case("ELG002: Debug template without arguments", Game("logger.Debug(\"Round started\");"));
        Case("ELG002: Debug template with an existing array", Game("var values = new object[] { hp }; logger.Debug(\"Hp {Hp}\", values);"));
        Case("ELG002: Info template is not reported", Game("logger.Info(\"Hp {Hp}\", hp);"));
        Fix("ELG002 fix: builder chain", new GuardWithIsEnabledCodeFix(), "ELG002",
            Game("logger.AtDebug().Log($\"hp {hp}\");"),
            Game("if (logger.IsEnabled(LogLevel.Debug, LogCategory.General)) logger.AtDebug().Log($\"hp {hp}\");"));
        Fix("ELG002 fix: category from At, even with string.Format", new GuardWithIsEnabledCodeFix(), "ELG002",
            Game("logger.At(LogLevel.Debug, LogCategory.General).Log(string.Format(\"{0}\", hp));"),
            Game("if (logger.IsEnabled(LogLevel.Debug, LogCategory.General)) logger.At(LogLevel.Debug, LogCategory.General).Log(string.Format(\"{0}\", hp));"));
        Fix("ELG002 fix: Debug template shorthand", new GuardWithIsEnabledCodeFix(), "ELG002",
            Game("logger.Debug(\"Hp {Hp}\", hp);"),
            Game("if (logger.IsEnabled(LogLevel.Debug, LogCategory.General)) logger.Debug(\"Hp {Hp}\", hp);"));
        Fix("ELG002 fix: direct Log call", new GuardWithIsEnabledCodeFix(), "ELG002",
            Game("logger.Log(LogLevel.Debug, LogCategory.General, $\"hp {hp}\");"),
            Game("if (logger.IsEnabled(LogLevel.Debug, LogCategory.General)) logger.Log(LogLevel.Debug, LogCategory.General, $\"hp {hp}\");"));
        Case("ELG006: missing argument", Game("logger.Info(\"Player {Name} took {Damage}\", hp);"), "ELG006");
        Case("ELG006: extra argument", Game("logger.Info(\"Player {Name}\", hp, hp);"), "ELG006");
        Case("ELG006: trailing exception is fine", Game("logger.Info(\"Save {Slot} failed\", hp, new System.Exception());"));
        Case("ELG006: exception overload is fine", Game("logger.Error(new System.Exception(), \"Save {Slot} failed\", hp);"));
        Case("ELG006: repeated hole name", Game("logger.Info(\"{A} then {A}\", hp, hp);"), "ELG006");
        Case("ELG006: escaped braces are not holes", Game("logger.Info(\"{{literal}} {Value}\", hp);"));
        Case("ELG006: invalid holes are literal", Game("logger.Info(\"{not a hole} {Value}\", hp);"));
        Case("ELG006: builder template", Game("logger.AtInfo().Log(\"{A} {B}\", hp);"), "ELG006");
        Case("ELG006: existing array is not counted", Game("var values = new object[] { hp }; logger.Info(\"{A} {B}\", values);"));
        Case("ELG006: non-constant template is not checked", Game("string t = \"{A}\"; logger.Info(t);"));

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
        Fix("ELG003 fix: adds Awake", new MoveLoggerToAwakeCodeFix(), "ELG003", @"
using EldritchGames.EldritchLogger.Core;
class Player : UnityEngine.MonoBehaviour { private readonly IEldritchLogger logger = ELoggerFactory.GetLogger<Player>(); }", @"
using EldritchGames.EldritchLogger.Core;
class Player : UnityEngine.MonoBehaviour { private IEldritchLogger logger; private void Awake() { logger = ELoggerFactory.GetLogger<Player>(); } }");
        Fix("ELG003 fix: prepends to an expression-bodied Awake", new MoveLoggerToAwakeCodeFix(), "ELG003", @"
using EldritchGames.EldritchLogger.Core;
class Player : UnityEngine.MonoBehaviour { IEldritchLogger logger = ELoggerFactory.GetLogger<Player>(); void Awake() => Init(); void Init() { } }", @"
using EldritchGames.EldritchLogger.Core;
class Player : UnityEngine.MonoBehaviour { IEldritchLogger logger; void Awake() { logger = ELoggerFactory.GetLogger<Player>(); Init(); } void Init() { } }");
        Fix("ELG003 fix: not offered for properties", new MoveLoggerToAwakeCodeFix(), "ELG003", @"
using EldritchGames.EldritchLogger.Core;
class Player : UnityEngine.MonoBehaviour { IEldritchLogger Logger { get; } = ELoggerFactory.GetLogger(""P""); }", null);

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

    private static string Coroutine(string body) => $@"
using EldritchGames.EldritchLogger.Core;
namespace Game
{{
    class Player
    {{
        IEldritchLogger logger;
        System.Collections.IEnumerator Round() {{ {body} }}
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

    /// <summary>
    /// Applies the first fix <paramref name="provider"/> offers for the <paramref name="id"/> diagnostic and compares the result
    /// with <paramref name="expected"/> (whitespace-insensitive). A null <paramref name="expected"/> means no fix may be offered.
    /// The fixed code must compile and no longer report <paramref name="id"/>.
    /// </summary>
    private static void Fix(string name, CodeFixProvider provider, string id, string source, string expected)
    {
        string outcome;
        try
        {
            outcome = ApplyFix(provider, id, source, expected);
        }
        catch (Exception ex)
        {
            outcome = ex.GetBaseException().Message;
        }

        Console.WriteLine($"{(outcome == null ? "PASS" : "FAIL")}  {name}{(outcome == null ? "" : "\n      " + outcome)}");
        if (outcome != null) Failures.Add(name);
    }

    private static string ApplyFix(CodeFixProvider provider, string id, string source, string expected)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(projectId, References())
            .AddDocument(DocumentId.CreateNewId(projectId), "Stubs.cs", Stubs)
            .AddDocument(documentId, "Test.cs", source);

        var document = solution.GetDocument(documentId);
        var diagnostic = Diagnose(document.Project).FirstOrDefault(d => d.Id == id && d.Location.SourceTree?.FilePath == "Test.cs");
        if (diagnostic == null) return $"no {id} diagnostic to fix";

        var actions = new List<CodeAction>();
        provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None)).Wait();
        if (expected == null) return actions.Count == 0 ? null : $"a fix was offered: {actions[0].Title}";
        if (actions.Count == 0) return "no fix offered";

        var operation = actions[0].GetOperationsAsync(CancellationToken.None).Result.OfType<ApplyChangesOperation>().Single();
        var fixedProject = operation.ChangedSolution.GetProject(projectId);
        var text = fixedProject.GetDocument(documentId).GetTextAsync().Result.ToString();
        if (Squash(text) != Squash(expected)) return $"got:\n{text}";

        var errors = fixedProject.GetCompilationAsync().Result.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0) return "fixed code does not compile: " + string.Join("; ", errors.Select(e => e.ToString()));
        if (Diagnose(fixedProject).Any(d => d.Id == id && d.Location.SourceTree?.FilePath == "Test.cs")) return $"{id} is still reported after the fix";
        return null;
    }

    private static ImmutableArray<Diagnostic> Diagnose(Project project) =>
        project.GetCompilationAsync().Result
               .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EldritchLoggerAnalyzer()))
               .GetAnalyzerDiagnosticsAsync().Result;

    private static string Squash(string code) => new string(code.Where(c => !char.IsWhiteSpace(c)).ToArray());

    private static IEnumerable<MetadataReference> References() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));

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
