using EldritchGames.EldritchLogger.Console.Parsing;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Parsing
{
    [TestFixture]
    public class CommandParserTests
    {
        [Test]
        public void Parse_ShouldFailOnEmptyTokens()
        {
            var parser = new CommandParser();
            var result = parser.Parse(new TokenList(new List<Token>()), "");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("No command entered.", result.ErrorMessage);
            Assert.IsNull(result.Command);
        }

        [Test]
        public void Parse_ShouldHandleUnexpectedToken()
        {
            var parser = new CommandParser();
            var tokens = new TokenList(new List<Token> { new Token(TokenType.Flag, "oops") });
            var result = parser.Parse(tokens, "oops");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Expected a command at the start.", result.ErrorMessage);
            Assert.IsNull(result.Command);
        }

        [Test]
        public void Parse_ShouldNotHang_OnManyTokens()
        {
            var tokens = new List<Token>();
            for (int i = 0; i < 10000; i++)
                tokens.Add(new Token(TokenType.Argument, "x"));

            var parser = new CommandParser();
            Assert.DoesNotThrow(() => parser.Parse(new TokenList(tokens), "x x x ..."));
        }
    }

    [TestFixture]
    public class CommandParserStressTests
    {
        [Test]
        public void Parse_ShouldNotHang_OnManyTokens()
        {
            var tokens = new List<Token>();
            for (int i = 0; i < 10000; i++)
                tokens.Add(new Token(TokenType.Argument, "x"));

            var parser = new CommandParser();
            Assert.DoesNotThrow(() => parser.Parse(new TokenList(tokens), "x x x ..."));
        }

        [Test]
        public void Parse_ShouldFailGracefully_OnUnexpectedToken()
        {
            var parser = new CommandParser();
            var tokens = new TokenList(new List<Token> { new Token(TokenType.Flag, "oops") });
            var result = parser.Parse(tokens, "oops");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Expected a command at the start.", result.ErrorMessage);
            Assert.IsNull(result.Command);
        }

        [Test]
        public void Parse_ShouldHandleFlagsWithoutValues()
        {
            var parser = new CommandParser();
            var tokens = new TokenList(new List<Token>
            {
                new Token(TokenType.Command, "test"),
                new Token(TokenType.Flag, "debug")
            });

            var result = parser.Parse(tokens, "test --debug");

            Assert.IsTrue(result.Success);
            Assert.AreEqual("test", result.Command.Name);
            Assert.AreEqual("true", result.Command.Flags["debug"]);
        }
    }
}
