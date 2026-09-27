using EldritchGames.EldritchLogger.Console.Parsing;
using NUnit.Framework;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Parsing
{
    [TestFixture]
    public class TokenBuilderTests
    {
        [Test]
        public void BuildToken_ShouldReturnArgument()
        {
            var stream = new CharacterStream("hello");
            var builder = new TokenBuilder();
            var token = builder.BuildToken(stream);

            Assert.AreEqual(TokenType.Argument, token.Type);
            Assert.AreEqual("hello", token.Value);
        }

        [Test]
        public void BuildToken_ShouldReturnStringLiteral()
        {
            var stream = new CharacterStream("\"orc warrior\"");
            var builder = new TokenBuilder();
            var token = builder.BuildToken(stream);

            Assert.AreEqual(TokenType.StringLiteral, token.Type);
            Assert.AreEqual("orc warrior", token.Value);
        }

        [Test]
        public void BuildToken_ShouldHandleUnclosedQuote()
        {
            var stream = new CharacterStream("\"orc warrior");
            var builder = new TokenBuilder();
            var token = builder.BuildToken(stream);

            Assert.AreEqual(TokenType.StringLiteral, token.Type);
            Assert.AreEqual("orc warrior", token.Value);
        }

        [Test]
        public void BuildToken_ShouldHandleEmptyInput()
        {
            var stream = new CharacterStream("");
            var builder = new TokenBuilder();
            var token = builder.BuildToken(stream);

            Assert.AreEqual(TokenType.Argument, token.Type);
            Assert.AreEqual(string.Empty, token.Value);
        }

        [Test]
        public void BuildToken_ShouldNotHang_OnLongInput()
        {
            var input = new string('a', 5000);
            var stream = new CharacterStream(input);
            var builder = new TokenBuilder();

            Assert.DoesNotThrow(() => builder.BuildToken(stream));
        }
    }

    [TestFixture]
    public class TokenBuilderStressTests
    {
        [Test]
        public void BuildToken_ShouldNotHang_OnExtremelyLongInput()
        {
            var input = new string('a', 20000);
            var stream = new CharacterStream(input);
            var builder = new TokenBuilder();

            Assert.DoesNotThrow(() => builder.BuildToken(stream));
        }

        [Test]
        public void BuildToken_ShouldHandleOnlyQuotes()
        {
            var stream = new CharacterStream("\"\"");
            var builder = new TokenBuilder();
            var token = builder.BuildToken(stream);

            Assert.AreEqual(TokenType.StringLiteral, token.Type);
            Assert.AreEqual(string.Empty, token.Value);
        }

        [Test]
        public void BuildToken_ShouldHandleUnclosedQuote()
        {
            var stream = new CharacterStream("\"unclosed");
            var builder = new TokenBuilder();
            var token = builder.BuildToken(stream);

            Assert.AreEqual(TokenType.StringLiteral, token.Type);
            Assert.AreEqual("unclosed", token.Value);
        }
    }

}