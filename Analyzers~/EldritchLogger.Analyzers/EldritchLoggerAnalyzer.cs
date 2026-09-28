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
    /// ELG002: Debug-level message built with interpolation/concatenation/string.Format without an IsEnabled check.
    /// ELG003: A MonoBehaviour field initialized with ELoggerFactory.GetLogger (runs before the logger exists).
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
            description: "Interpolated strings, concatenation and string.Format allocate before the logger can discard the entry.");

        public static readonly DiagnosticDescriptor LoggerInFieldInitializer = new(
            "ELG003",
            "Logger obtained in a MonoBehaviour field initializer",
            "Initialize '{0}' in Awake(): MonoBehaviour field initializers can run before the logger is installed and capture a no-op logger",
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(UseLogger, GuardExpensiveDebugMessage, LoggerInFieldInitializer);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                // Only analyze code that can see the logger.
                if (start.Compilation.GetTypeByMetadataName(LoggerInterface) == null) return;
                start.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            });
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
