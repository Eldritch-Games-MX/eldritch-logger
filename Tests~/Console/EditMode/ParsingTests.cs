using EldritchGames.EldritchLogger.Console.Parsing;
using NUnit.Framework;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ParsingTests
    {
        private static Token[] Lex(string input) => new Lexer().Tokenize(input).AllTokens.ToArray();

        [Test]
        public void Lexer_ClassifiesTokens()
        {
            var tokens = Lex("say \"hello world\" --loud -v 42 -5 -");

            Assert.That(tokens.Select(t => t.Type), Is.EqualTo(new[]
            {
                TokenType.Command, TokenType.StringLiteral, TokenType.Flag, TokenType.Flag,
                TokenType.Argument, TokenType.Argument, TokenType.Argument
            }));
            Assert.That(tokens.Select(t => t.Value), Is.EqualTo(new[] { "say", "hello world", "loud", "v", "42", "-5", "-" }));
        }

        [Test]
        public void Lexer_HandlesNullAndWhitespace()
        {
            Assert.That(Lex(null), Is.Empty);
            Assert.That(Lex("   "), Is.Empty);
        }

        [Test]
        public void Parser_KeepsArgumentTokensInOrder()
        {
            var command = ConsoleTestHelpers.Parse("history --limit 2 extra");

            Assert.That(command.Name, Is.EqualTo("history"));
            Assert.That(command.ArgumentTokens.Select(t => t.ToSourceText()), Is.EqualTo(new[] { "--limit", "2", "extra" }));
            Assert.That(command.RawText, Is.EqualTo("history --limit 2 extra"));
        }

        [Test]
        public void Parser_FailsOnEmptyInput()
        {
            var result = new CommandParser().Parse(new Lexer().Tokenize(""), "");
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void FromTokens_RebuildsANestedCommand_PreservingQuotes()
        {
            var outer = ConsoleTestHelpers.Parse("repeat 2 say \"a b\" --loud");
            var inner = ParsedCommand.FromTokens(outer.ArgumentTokens.Skip(1).ToArray());

            Assert.That(inner.Name, Is.EqualTo("say"));
            Assert.That(inner.ArgumentTokens.Select(t => t.Value), Is.EqualTo(new[] { "a b", "loud" }));
            Assert.That(inner.RawText, Is.EqualTo("say \"a b\" --loud"));
        }
    }
}
