using EldritchGames.EldritchLogger.Console.Parsing;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Parsing
{
    [TestFixture]
    public class LexerTests
    {
        [Test]
        public void Tokenize_ShouldHandleEmptyInput()
        {
            var lexer = new Lexer();
            var tokens = lexer.Tokenize("");

            Assert.AreEqual(0, tokens.Count);
        }

        [Test]
        public void Tokenize_ShouldNotHang_OnLongInput()
        {
            var lexer = new Lexer();
            var input = new string('a', 10000);
            Assert.DoesNotThrow(() => lexer.Tokenize(input));
        }

        [Test]
        public void Tokenize_ShouldReturnCommandToken()
        {
            var lexer = new Lexer();
            var tokens = lexer.Tokenize("spawn_enemy");

            var first = tokens.Peek();
            Assert.AreEqual(TokenType.Command, first.Type);
            Assert.AreEqual("spawn_enemy", first.Value);
        }
    }
    [TestFixture]
    public class LexerStressTests
    {
        [Test]
        public void Tokenize_ShouldNotHang_OnExtremelyLongInput()
        {
            var lexer = new Lexer();
            var input = string.Join(" ", Enumerable.Repeat("word", 5000));
            Assert.DoesNotThrow(() => lexer.Tokenize(input));
        }

        [Test]
        public void Tokenize_ShouldHandleEmptyInput()
        {
            var lexer = new Lexer();
            var tokens = lexer.Tokenize("");
            Assert.AreEqual(0, tokens.AllTokens.Count());
        }

        [Test]
        public void Tokenize_ShouldHandleMixedQuotedAndFlags()
        {
            var lexer = new Lexer();
            var tokens = lexer.Tokenize("cmd \"quoted arg\" --flag=value -f");

            var list = tokens.AllTokens.ToList();
            Assert.AreEqual(TokenType.Command, list[0].Type);
            Assert.AreEqual(TokenType.StringLiteral, list[1].Type);
            Assert.AreEqual(TokenType.Flag, list[2].Type);
            Assert.AreEqual(TokenType.Flag, list[3].Type);
        }
    }

}