using EldritchGames.EldritchLogger.Console.Parsing;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Parsing
{
    [TestFixture]
    public class CharacterStreamTests
    {
        [Test]
        public void Peek_ShouldReturnDefaultChar_WhenEmpty()
        {
            var stream = new CharacterStream("");
            Assert.AreEqual('\0', stream.Peek());
        }

        [Test]
        public void Next_ShouldReturnDefaultChar_WhenEmpty()
        {
            var stream = new CharacterStream("");
            Assert.AreEqual('\0', stream.Next());
        }

        [Test]
        public void SkipWhitespace_ShouldNotHang_OnAllSpaces()
        {
            var stream = new CharacterStream("     ");
            Assert.DoesNotThrow(() => stream.SkipWhitespace());
            Assert.IsFalse(stream.HasNext);
        }
    }
    [TestFixture]
    public class CharacterStreamStressTests
    {
        [Test]
        public void SkipWhitespace_ShouldHandleVeryLongWhitespace()
        {
            var input = new string(' ', 10000);
            var stream = new CharacterStream(input);
            Assert.DoesNotThrow(() => stream.SkipWhitespace());
            Assert.IsFalse(stream.HasNext);
        }

        [Test]
        public void PeekAndNext_ShouldNotCrash_OnEmptyInput()
        {
            var stream = new CharacterStream("");
            Assert.AreEqual('\0', stream.Peek());
            Assert.AreEqual('\0', stream.Next());
        }
    }


}