using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EldritchLogger.Analyzers.CodeFixes
{
    /// <summary>ELG002: wraps the Debug logging statement in <c>if (logger.IsEnabled(LogLevel.Debug, category))</c>.</summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GuardWithIsEnabledCodeFix)), Shared]
    public sealed class GuardWithIsEnabledCodeFix : CodeFixProvider
    {
        private const string CoreNamespace = "EldritchGames.EldritchLogger.Core";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("ELG002");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            // The diagnostic is on the message argument; the logging call is the invocation that argument belongs to.
            var argument = node?.AncestorsAndSelf().OfType<ArgumentSyntax>().FirstOrDefault();
            var call = OutermostCall(argument?.Parent?.Parent as InvocationExpressionSyntax);
            if (!(call?.Parent is ExpressionStatementSyntax statement)) return;
            if (!TryFindLoggerAndCategory(call, out var logger, out var category)) return;

            context.RegisterCodeFix(
                CodeAction.Create("Wrap in 'if (logger.IsEnabled(...))'",
                    ct => WrapAsync(context.Document, statement, logger, category, ct),
                    nameof(GuardWithIsEnabledCodeFix)),
                context.Diagnostics);
        }

        /// <summary>The whole logging call (<c>logger.AtDebug().Log(...)</c>), not an inner link of the chain.</summary>
        private static InvocationExpressionSyntax OutermostCall(InvocationExpressionSyntax call)
        {
            while (call?.Parent is MemberAccessExpressionSyntax access && access.Parent is InvocationExpressionSyntax outer)
                call = outer;
            return call;
        }

        /// <summary>
        /// Walks the call chain from the outermost call inwards: <c>logger.Debug(...)</c>, <c>logger.Log(Debug, cat, ...)</c>,
        /// <c>logger.LogTemplate(Debug, cat, ...)</c>, or <c>logger.AtDebug(cat)/At(Debug, cat)...[.Category(c)]...Log(...)</c>.
        /// </summary>
        internal static bool TryFindLoggerAndCategory(InvocationExpressionSyntax call, out ExpressionSyntax logger, out ExpressionSyntax category)
        {
            logger = null;
            category = null;
            ExpressionSyntax overriddenCategory = null;

            for (var current = call; current?.Expression is MemberAccessExpressionSyntax access; current = access.Expression as InvocationExpressionSyntax)
            {
                var args = current.ArgumentList.Arguments;
                switch (access.Name.Identifier.Text)
                {
                    case "Category" when overriddenCategory == null && args.Count == 1:
                        overriddenCategory = args[0].Expression; // the last .Category() in the chain wins
                        break;
                    case "AtDebug":
                        logger = access.Expression;
                        category = overriddenCategory ?? (args.Count > 0 ? args[0].Expression : null);
                        break;
                    case "At":
                        logger = access.Expression;
                        category = overriddenCategory ?? (args.Count > 1 ? args[1].Expression : null);
                        break;
                    case "Debug" when current == call:
                        logger = access.Expression;
                        break;
                    case "Log" when current == call && args.Count >= 3 && !(access.Expression is InvocationExpressionSyntax):
                    case "LogTemplate" when current == call && args.Count >= 3:
                        logger = access.Expression;
                        category = args[1].Expression;
                        break;
                    default:
                        continue;
                }
                break;
            }

            // The logger expression is repeated in the condition, so it must be free of side effects.
            return logger is IdentifierNameSyntax || logger is MemberAccessExpressionSyntax || logger is ThisExpressionSyntax;
        }

        private static async Task<Document> WrapAsync(Document document, ExpressionStatementSyntax statement,
                                                      ExpressionSyntax logger, ExpressionSyntax category, CancellationToken cancellation)
        {
            var root = await document.GetSyntaxRootAsync(cancellation).ConfigureAwait(false);

            var levelDebug = ParseExpression($"global::{CoreNamespace}.LogLevel.Debug").WithAdditionalAnnotations(Simplifier.Annotation);
            var categoryExpression = category?.WithoutTrivia()
                                     ?? ParseExpression($"global::{CoreNamespace}.LogCategory.General").WithAdditionalAnnotations(Simplifier.Annotation);

            var condition = InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, logger.WithoutTrivia(), IdentifierName("IsEnabled")),
                ArgumentList(SeparatedList(new[] { Argument(levelDebug), Argument(categoryExpression) })));

            var guarded = IfStatement(condition, statement.WithoutLeadingTrivia())
                .WithLeadingTrivia(statement.GetLeadingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            return document.WithSyntaxRoot(root.ReplaceNode(statement, guarded));
        }
    }
}
