using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EldritchLogger.Analyzers.CodeFixes
{
    /// <summary>
    /// ELG004: rewrites <c>logger.Info($"Player {player.Name} took {damage:0.0}")</c> (or the same built with <c>+</c>)
    /// as <c>logger.Info("Player {Name} took {Damage:0.0}", player.Name, damage)</c>.
    /// </summary>
    /// <remarks>
    /// Literal text is kept exactly as the logger saw it at runtime: <c>$"{{Max}}"</c> produced <c>{Max}</c>, which the
    /// logger already read as a hole, so it stays a hole. When such holes exist and the call already passes arguments,
    /// their order relative to the new values is ambiguous, so no fix is offered.
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseTemplateHolesCodeFix)), Shared]
    public sealed class UseTemplateHolesCodeFix : CodeFixProvider
    {
        private static readonly Regex LiteralHole = new(@"(?<!\{)\{\s*[@$]?(?<name>[A-Za-z0-9_]+)\s*(:[^}]*)?\}", RegexOptions.Compiled);

        private sealed class Part
        {
            public string Text;              // literal text (unescaped), or null for a value
            public ExpressionSyntax Value;
            public string Format;
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("ELG004");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            var template = node is ArgumentSyntax wrapped ? wrapped.Expression : node as ExpressionSyntax;
            if (!(template?.Parent is ArgumentSyntax argument) || argument.NameColon != null) return;
            if (!(argument.Parent is ArgumentListSyntax list)) return;

            // Values are inserted as positional arguments after the template, so everything after it must be positional too.
            int index = list.Arguments.IndexOf(argument);
            if (list.Arguments.Skip(index + 1).Any(a => a.NameColon != null)) return;

            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var parts = Split(template, model, context.CancellationToken);
            if (parts == null || !parts.Any(p => p.Value != null)) return;

            var literalText = string.Concat(parts.Where(p => p.Value == null).Select(p => p.Text));
            bool hasLiteralHoles = LiteralHole.IsMatch(literalText);
            if (hasLiteralHoles && index < list.Arguments.Count - 1) return;

            context.RegisterCodeFix(
                CodeAction.Create("Use message template holes",
                    ct => RewriteAsync(context.Document, list, index, parts, ct),
                    nameof(UseTemplateHolesCodeFix)),
                context.Diagnostics);
        }

        /// <summary>The literal text and values of an interpolated string or a string concatenation, or null if unsupported.</summary>
        private static List<Part> Split(ExpressionSyntax template, SemanticModel model, CancellationToken cancellation)
        {
            var parts = new List<Part>();
            switch (template)
            {
                case InterpolatedStringExpressionSyntax interpolated:
                    if (!interpolated.StringStartToken.IsKind(SyntaxKind.InterpolatedStringStartToken) &&
                        !interpolated.StringStartToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken))
                        return null; // raw string literals: left alone
                    foreach (var content in interpolated.Contents)
                    {
                        if (content is InterpolatedStringTextSyntax text)
                            // ValueText resolves escape sequences but keeps {{ and }}; the runtime string has single braces.
                            parts.Add(new Part { Text = text.TextToken.ValueText.Replace("{{", "{").Replace("}}", "}") });
                        else if (content is InterpolationSyntax hole)
                            parts.Add(new Part { Value = hole.Expression, Format = hole.FormatClause?.FormatStringToken.ValueText });
                    }
                    return parts;

                case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression):
                    Flatten(binary, model, cancellation, parts);
                    return parts;

                default:
                    return null;
            }
        }

        private static void Flatten(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellation, List<Part> parts)
        {
            // Only string concatenation is flattened: in "a" + (x + 1) the parentheses keep (x + 1) one value.
            if (expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression) &&
                model.GetTypeInfo(binary, cancellation).Type?.SpecialType == SpecialType.System_String)
            {
                Flatten(binary.Left, model, cancellation, parts);
                Flatten(binary.Right, model, cancellation, parts);
                return;
            }

            if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                parts.Add(new Part { Text = literal.Token.ValueText });
            else
                parts.Add(new Part { Value = expression });
        }

        private static async Task<Document> RewriteAsync(Document document, ArgumentListSyntax list, int index, List<Part> parts,
                                                         CancellationToken cancellation)
        {
            var root = await document.GetSyntaxRootAsync(cancellation).ConfigureAwait(false);

            var template = new StringBuilder();
            var values = new List<ArgumentSyntax>();
            // Names already used by holes written in the literal text.
            var used = new HashSet<string>(LiteralHole.Matches(string.Concat(parts.Where(p => p.Value == null).Select(p => p.Text)))
                                                      .Cast<Match>().Select(m => m.Groups["name"].Value));
            foreach (var part in parts)
            {
                if (part.Value == null)
                {
                    template.Append(part.Text); // exactly the text the logger received before
                    continue;
                }

                var name = UniqueName(HoleName(part.Value), used);
                template.Append('{').Append(name);
                if (!string.IsNullOrEmpty(part.Format)) template.Append(':').Append(part.Format);
                template.Append('}');
                values.Add(Argument(Unwrap(part.Value).WithoutTrivia()));
            }

            var original = list.Arguments[index];
            var literal = Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(template.ToString())))
                .WithTriviaFrom(original);

            var arguments = list.Arguments.Take(index).Append(literal).Concat(values).Concat(list.Arguments.Skip(index + 1));
            var newList = list.WithArguments(SeparatedList(arguments)).WithAdditionalAnnotations(Formatter.Annotation);
            return document.WithSyntaxRoot(root.ReplaceNode(list, newList));
        }

        private static ExpressionSyntax Unwrap(ExpressionSyntax expression) =>
            expression is ParenthesizedExpressionSyntax parenthesized && !(parenthesized.Expression is ConditionalExpressionSyntax)
                ? parenthesized.Expression
                : expression;

        /// <summary><c>damage</c> → Damage, <c>player.Name</c> → Name, <c>_hp</c> / <c>m_Hp</c> → Hp, <c>GetScore()</c> → Score.</summary>
        internal static string HoleName(ExpressionSyntax expression)
        {
            string raw;
            switch (Unwrap(expression))
            {
                case IdentifierNameSyntax identifier: raw = identifier.Identifier.Text; break;
                case MemberAccessExpressionSyntax access: raw = access.Name.Identifier.Text; break;
                case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax method }: raw = method.Name.Identifier.Text; break;
                case InvocationExpressionSyntax { Expression: IdentifierNameSyntax function }: raw = function.Identifier.Text; break;
                default: raw = "Value"; break;
            }

            if (raw.StartsWith("m_")) raw = raw.Substring(2);
            raw = raw.TrimStart('_');
            if (raw.Length > 3 && raw.StartsWith("Get") && char.IsUpper(raw[3])) raw = raw.Substring(3);
            if (raw.Length == 0) raw = "Value";
            return char.ToUpperInvariant(raw[0]) + raw.Substring(1);
        }

        private static string UniqueName(string name, HashSet<string> used)
        {
            var candidate = name;
            for (int n = 2; !used.Add(candidate); n++) candidate = name + n;
            return candidate;
        }
    }
}
