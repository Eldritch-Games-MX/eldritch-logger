using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace EldritchLogger.Analyzers
{
    /// <summary>
    /// ELG001: UnityEngine.Debug.Log* used instead of the logger.
    /// ELG002: Debug-level message built with interpolation/concatenation/string.Format, or a Debug template call with
    ///         arguments, without an IsEnabled check.
    /// ELG003: A MonoBehaviour field initialized with ELoggerFactory.GetLogger (runs before the logger exists).
    /// ELG004: A message template built with interpolation/concatenation (loses the structured properties).
    /// ELG005: A log scope kept open across a coroutine yield (it leaks to unrelated main-thread logs).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EldritchLoggerAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "EldritchLogger";
        private const string LoggerNamespace = "EldritchGames.EldritchLogger";
        private const string ExtensionsType = LoggerNamespace + ".Core.EldritchLoggerExtensions";
        private const string BuilderType = LoggerNamespace + ".Builder.ILogBuilder";
        private const string LoggerInterface = LoggerNamespace + ".Core.IEldritchLogger";
        private const string FactoryType = LoggerNamespace + ".Core.ELoggerFactory";
        private const string LogLevelType = LoggerNamespace + ".Core.LogLevel";

        public static readonly DiagnosticDescriptor UseLogger = new(
            "ELG001",
            "Use the Eldritch logger instead of UnityEngine.Debug",
            "'Debug.{0}' bypasses EldritchLogger filtering, categories and sinks; use an IEldritchLogger",
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: "Log through IEldritchLogger (ELoggerFactory.GetLogger<T>()) so entries are filtered, categorized and written to every sink.");

        public static readonly DiagnosticDescriptor GuardExpensiveDebugMessage = new(
            "ELG002",
            "Debug message is built even when Debug logging is disabled",
            "This Debug-level message is built with {0} even when Debug logging is disabled; wrap it in 'if (logger.IsEnabled(LogLevel.Debug, category))'",
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: "Interpolated strings, concatenation and string.Format allocate before the logger can discard the entry. " +
                         "Message templates (logger.Debug(\"... {Value}\", value)) avoid formatting and capture properties, but their " +
                         "argument array and boxed values are still allocated at the call site, so hot paths still need the IsEnabled check.");

        public static readonly DiagnosticDescriptor InterpolatedTemplate = new(
            "ELG004",
            "Message template built with interpolation or concatenation",
            "This message template is built with {0}: put the values in holes and pass them as arguments (\"Player {{Name}} joined\", name) so they are captured as properties",
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: "Interpolating into a template loses the structured properties, and braces inside the values are read as template holes.");

        public static readonly DiagnosticDescriptor LoggerInFieldInitializer = new(
            "ELG003",
            "Logger obtained in a MonoBehaviour field initializer",
            "Initialize '{0}' in Awake(): MonoBehaviour field initializers can run before the logger is installed and capture a no-op logger",
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ScopeAcrossYield = new(
            "ELG005",
            "Log scope kept open across a yield",
            "This 'yield' keeps a log scope open: coroutines resume outside the execution context, so the scope applies to unrelated logs until the coroutine resumes; close the scope before yielding",
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: "Unity resumes coroutines from native code without restoring the execution context. A scope opened before a yield stays active on the main thread while the coroutine is suspended.");

        private const string LogScopeType = LoggerNamespace + ".Pipeline.LogScope";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(UseLogger, GuardExpensiveDebugMessage, LoggerInFieldInitializer, InterpolatedTemplate, ScopeAcrossYield);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                // Only analyze code that can see the logger.
                if (start.Compilation.GetTypeByMetadataName(LoggerInterface) == null) return;
                start.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
                start.RegisterSyntaxNodeAction(AnalyzeYield, SyntaxKind.YieldReturnStatement);
            });
        }

        private static void AnalyzeYield(SyntaxNodeAnalysisContext context)
        {
            var yield = (YieldStatementSyntax)context.Node;

            foreach (var ancestor in yield.Ancestors())
            {
                if (ancestor is MemberDeclarationSyntax || ancestor is AnonymousFunctionExpressionSyntax || ancestor is LocalFunctionStatementSyntax)
                    break; // the iterator's own body ends here

                // using (logger.BeginScope(...)) { ... yield ... }
                if (ancestor is UsingStatementSyntax usingStatement && usingStatement.Statement.Span.Contains(yield.Span) &&
                    OpensScope(context, (SyntaxNode)usingStatement.Expression ?? usingStatement.Declaration))
                {
                    Report(context, yield);
                    return;
                }

                // using var scope = logger.BeginScope(...); ... yield ...
                if (ancestor is BlockSyntax block)
                {
                    foreach (var statement in block.Statements)
                    {
                        if (statement.SpanStart >= yield.SpanStart) break;
                        if (statement is LocalDeclarationStatementSyntax local && local.UsingKeyword != default &&
                            OpensScope(context, local.Declaration))
                        {
                            Report(context, yield);
                            return;
                        }
                    }
                }
            }
        }

        private static void Report(SyntaxNodeAnalysisContext context, YieldStatementSyntax yield) =>
            context.ReportDiagnostic(Diagnostic.Create(ScopeAcrossYield, yield.GetLocation()));

        /// <summary>True when <paramref name="node"/> calls <c>BeginScope</c> or <c>LogScope.Push</c>.</summary>
        private static bool OpensScope(SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            if (node == null) return false;
            foreach (var call in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (context.SemanticModel.GetSymbolInfo(call, context.CancellationToken).Symbol is not IMethodSymbol m) continue;
                var type = m.ContainingType?.ToDisplayString();
                if ((type == ExtensionsType && m.Name == "BeginScope") || (type == LogScopeType && m.Name == "Push"))
                    return true;
            }
            return false;
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
                return;

            if (IsInLoggerPackage(context)) return;

            var containingType = method.ContainingType?.ToDisplayString();

            if (containingType == "UnityEngine.Debug" && method.Name.StartsWith("Log"))
            {
                // Editor tooling legitimately writes to the Unity Console.
                if (IsEditorAssembly(context.Compilation.AssemblyName)) return;
                context.ReportDiagnostic(Diagnostic.Create(UseLogger, invocation.GetLocation(), method.Name));
                return;
            }

            if (containingType == FactoryType && method.Name == "GetLogger")
            {
                CheckFieldInitializer(context, invocation);
                return;
            }

            if (TemplateArgument(invocation, method, containingType) is { } template)
            {
                var cost = DescribeCost(context, template);
                if (cost != null)
                    context.ReportDiagnostic(Diagnostic.Create(InterpolatedTemplate, template.GetLocation(), cost));
                else
                    CheckDebugTemplate(context, invocation, method, containingType, template);
                return;
            }

            if (method.Name == "Log")
                CheckDebugMessage(context, invocation, method, containingType);
        }

        /// <summary>Assembly-CSharp-Editor, *.Editor and *.Editor.* assemblies.</summary>
        internal static bool IsEditorAssembly(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName)) return false;
            if (assemblyName.StartsWith("Assembly-CSharp-Editor")) return true;
            return assemblyName.Split('.').Any(part => part == "Editor");
        }

        private static readonly string[] TemplateMethods = { "Debug", "Info", "Warning", "Error", "Critical", "LogTemplate" };

        /// <summary>The template argument of a message-template call, or null for other calls.</summary>
        private static ExpressionSyntax TemplateArgument(InvocationExpressionSyntax invocation, IMethodSymbol method, string containingType)
        {
            bool isTemplateCall =
                (containingType == ExtensionsType && TemplateMethods.Contains(method.Name)) ||
                (containingType == BuilderType && method.Name == "Log" && method.Parameters.Length == 2);
            if (!isTemplateCall) return null;

            var args = invocation.ArgumentList.Arguments;
            foreach (var arg in args)
                if (arg.NameColon?.Name.Identifier.Text == "template") return arg.Expression;

            // Extension calls resolve to the reduced method, whose parameters exclude "this".
            int index = method.Parameters.IndexOf(method.Parameters.FirstOrDefault(p => p.Name == "template"));
            return index >= 0 && index < args.Count ? args[index].Expression : null;
        }

        private static bool IsInLoggerPackage(SyntaxNodeAnalysisContext context)
        {
            var type = context.ContainingSymbol?.ContainingType ?? context.ContainingSymbol as INamedTypeSymbol;
            var ns = type?.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            return ns == LoggerNamespace || ns.StartsWith(LoggerNamespace + ".");
        }

        private static void CheckFieldInitializer(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
        {
            var declarator = invocation.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            var field = declarator?.Parent?.Parent as FieldDeclarationSyntax;
            var property = invocation.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            bool inInitializer = field != null || (property?.Initializer != null && property.Initializer.Span.Contains(invocation.Span));
            if (!inInitializer) return;

            var type = context.ContainingSymbol?.ContainingType ?? context.ContainingSymbol as INamedTypeSymbol;
            if (!DerivesFrom(type, "UnityEngine.MonoBehaviour")) return;

            var name = declarator?.Identifier.Text ?? property?.Identifier.Text ?? "logger";
            context.ReportDiagnostic(Diagnostic.Create(LoggerInFieldInitializer, invocation.GetLocation(), name));
        }

        private static void CheckDebugMessage(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation,
                                              IMethodSymbol method, string containingType)
        {
            ExpressionSyntax message;
            if (containingType == BuilderType)
            {
                // builder.Log(message): Debug level when the chain started with AtDebug() / At(LogLevel.Debug).
                if (invocation.ArgumentList.Arguments.Count != 1 || !ChainStartsAtDebug(context, invocation)) return;
                message = invocation.ArgumentList.Arguments[0].Expression;
            }
            else if (containingType == ExtensionsType)
            {
                // logger.Log(LogLevel.Debug, category, message, ...)
                var args = invocation.ArgumentList.Arguments;
                if (args.Count < 3 || !IsDebugLevel(context, args[0].Expression)) return;
                message = args[2].Expression;
            }
            else
            {
                return;
            }

            var cost = DescribeCost(context, message);
            if (cost == null || IsGuardedByIsEnabled(context, invocation)) return;

            context.ReportDiagnostic(Diagnostic.Create(GuardExpensiveDebugMessage, message.GetLocation(), cost));
        }

        /// <summary>
        /// A Debug-level template call with arguments allocates its params array (and boxes value types)
        /// before the logger can discard the entry.
        /// </summary>
        private static void CheckDebugTemplate(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation,
                                               IMethodSymbol method, string containingType, ExpressionSyntax template)
        {
            var args = invocation.ArgumentList.Arguments;
            bool isDebug =
                (containingType == ExtensionsType && method.Name == "Debug") ||
                (containingType == ExtensionsType && method.Name == "LogTemplate" && args.Count > 0 && IsDebugLevel(context, args[0].Expression)) ||
                (containingType == BuilderType && ChainStartsAtDebug(context, invocation));
            if (!isDebug) return;

            int templateIndex = -1;
            for (int i = 0; i < args.Count; i++)
                if (args[i].Expression == template) templateIndex = i;
            var values = args.Skip(templateIndex + 1).Select(a => a.Expression).ToList();
            if (values.Count == 0) return;

            // An existing object[] passed as the params array allocates nothing new.
            if (values.Count == 1 && context.SemanticModel.GetTypeInfo(values[0], context.CancellationToken).Type is IArrayTypeSymbol)
                return;

            if (IsGuardedByIsEnabled(context, invocation)) return;

            bool boxes = values.Any(v => context.SemanticModel.GetTypeInfo(v, context.CancellationToken).Type?.IsValueType == true);
            var cost = boxes ? "a params array and boxed arguments" : "a params array";
            context.ReportDiagnostic(Diagnostic.Create(GuardExpensiveDebugMessage, template.GetLocation(), cost));
        }

        private static bool ChainStartsAtDebug(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
        {
            ExpressionSyntax current = invocation;
            while (current is InvocationExpressionSyntax call && call.Expression is MemberAccessExpressionSyntax access)
            {
                if (context.SemanticModel.GetSymbolInfo(call, context.CancellationToken).Symbol is IMethodSymbol m &&
                    m.ContainingType?.ToDisplayString() == ExtensionsType)
                {
                    if (m.Name == "AtDebug") return true;
                    if (m.Name == "At")
                        return call.ArgumentList.Arguments.Count > 0 && IsDebugLevel(context, call.ArgumentList.Arguments[0].Expression);
                    return false;
                }
                current = access.Expression;
            }
            return false;
        }

        private static bool IsDebugLevel(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            var constant = context.SemanticModel.GetConstantValue(expression, context.CancellationToken);
            var type = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type;
            return constant.HasValue && type?.ToDisplayString() == LogLevelType && System.Convert.ToInt32(constant.Value) == 0;
        }

        /// <summary>A short description of why building the message costs something, or null if it is cheap.</summary>
        private static string DescribeCost(SyntaxNodeAnalysisContext context, ExpressionSyntax message)
        {
            if (context.SemanticModel.GetConstantValue(message, context.CancellationToken).HasValue) return null;

            switch (message)
            {
                case InterpolatedStringExpressionSyntax interpolated
                    when interpolated.Contents.OfType<InterpolationSyntax>().Any():
                    return "string interpolation";
                case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression):
                    return "string concatenation";
                case InvocationExpressionSyntax call
                    when context.SemanticModel.GetSymbolInfo(call, context.CancellationToken).Symbol is IMethodSymbol m &&
                         m.ContainingType?.SpecialType == SpecialType.System_String && m.Name == "Format":
                    return "string.Format";
                default:
                    return null;
            }
        }

        private static bool IsGuardedByIsEnabled(SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            foreach (var ifStatement in node.Ancestors().OfType<IfStatementSyntax>())
            {
                if (!ifStatement.Statement.Span.Contains(node.Span)) continue; // in the else branch
                foreach (var call in ifStatement.Condition.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
                {
                    if (context.SemanticModel.GetSymbolInfo(call, context.CancellationToken).Symbol is IMethodSymbol m &&
                        m.Name == "IsEnabled")
                        return true;
                }
            }
            return false;
        }

        private static bool DerivesFrom(INamedTypeSymbol type, string baseTypeName)
        {
            for (var current = type?.BaseType; current != null; current = current.BaseType)
                if (current.ToDisplayString() == baseTypeName) return true;
            return false;
        }
    }
}
