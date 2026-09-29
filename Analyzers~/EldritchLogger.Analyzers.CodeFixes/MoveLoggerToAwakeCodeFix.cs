using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EldritchLogger.Analyzers.CodeFixes
{
    /// <summary>ELG003: moves a logger field's initializer into <c>Awake()</c>, creating the method when needed.</summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveLoggerToAwakeCodeFix)), Shared]
    public sealed class MoveLoggerToAwakeCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("ELG003");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            var declarator = node?.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault();

            // Fields only: a get-only auto-property cannot be assigned in Awake.
            if (!(declarator?.Parent?.Parent is FieldDeclarationSyntax field) || field.Declaration.Variables.Count != 1) return;
            if (!(field.Parent is ClassDeclarationSyntax type) || declarator.Initializer == null) return;

            context.RegisterCodeFix(
                CodeAction.Create($"Initialize '{declarator.Identifier.Text}' in Awake()",
                    ct => MoveAsync(context.Document, type, field, declarator, ct),
                    nameof(MoveLoggerToAwakeCodeFix)),
                context.Diagnostics);
        }

        private static async Task<Document> MoveAsync(Document document, ClassDeclarationSyntax type, FieldDeclarationSyntax field,
                                                      VariableDeclaratorSyntax declarator, CancellationToken cancellation)
        {
            var root = await document.GetSyntaxRootAsync(cancellation).ConfigureAwait(false);

            // The field keeps its declaration without the initializer; a readonly field could not be assigned in Awake.
            var newField = field
                .WithDeclaration(field.Declaration.WithVariables(SingletonSeparatedList(declarator.WithInitializer(null))))
                .WithModifiers(TokenList(field.Modifiers.Where(m => !m.IsKind(SyntaxKind.ReadOnlyKeyword))))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var assignment = ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(declarator.Identifier.Text), declarator.Initializer.Value.WithoutTrivia()))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var awake = type.Members.OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "Awake" && m.ParameterList.Parameters.Count == 0);

            ClassDeclarationSyntax newType;
            if (awake != null)
            {
                newType = type.ReplaceNodes(new SyntaxNode[] { field, awake },
                    (original, _) => original == field ? newField : (SyntaxNode)PrependTo((MethodDeclarationSyntax)original, assignment));
            }
            else
            {
                var method = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "Awake")
                    .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
                    .WithBody(Block(assignment))
                    .WithAdditionalAnnotations(Formatter.Annotation);
                newType = type.ReplaceNode(field, newField);
                var placed = newType.Members.OfType<FieldDeclarationSyntax>()
                    .First(f => f.Declaration.Variables.Any(v => v.Identifier.Text == declarator.Identifier.Text));
                newType = newType.InsertNodesAfter(placed, new[] { method });
            }

            return document.WithSyntaxRoot(root.ReplaceNode(type, newType));
        }

        /// <summary>Puts <paramref name="statement"/> first in the method, turning an expression body into a block.</summary>
        private static MethodDeclarationSyntax PrependTo(MethodDeclarationSyntax method, StatementSyntax statement)
        {
            if (method.Body != null)
                return method.WithBody(method.Body.WithStatements(method.Body.Statements.Insert(0, statement)));

            var existing = ExpressionStatement(method.ExpressionBody.Expression);
            return method.WithExpressionBody(null)
                         .WithSemicolonToken(default)
                         .WithBody(Block(statement, existing))
                         .WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
