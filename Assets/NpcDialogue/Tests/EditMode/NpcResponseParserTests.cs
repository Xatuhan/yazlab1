using NUnit.Framework;

namespace YazLab.NpcDialogue.Tests
{
    public class NpcResponseParserTests
    {
        private const int MaxReply = 400;
        private NpcResponseParser _parser;

        [SetUp]
        public void SetUp()
        {
            _parser = new NpcResponseParser(MaxReply);
        }

        [Test]
        public void Parse_Allow()
        {
            Assert.IsTrue(_parser.TryParse("{\"action\":\"allow\",\"reply\":\"Geç, yolcu.\"}", out NpcDecision d));
            Assert.AreEqual(NpcAction.Allow, d.Action);
            Assert.AreEqual("Geç, yolcu.", d.Reply);
        }

        [Test]
        public void Parse_Attack()
        {
            Assert.IsTrue(_parser.TryParse("{\"action\":\"attack\",\"reply\":\"Al bakalım!\"}", out NpcDecision d));
            Assert.AreEqual(NpcAction.Attack, d.Action);
        }

        // Modelin uydurduğu bir eylem kapıyı açamaz.
        [TestCase("open")]
        [TestCase("izin ver")]
        [TestCase("")]
        public void Parse_UnknownAction_IsDeny(string action)
        {
            string json = "{\"action\":\"" + action + "\",\"reply\":\"Olmaz.\"}";

            Assert.IsTrue(_parser.TryParse(json, out NpcDecision d));
            Assert.AreEqual(NpcAction.Deny, d.Action);
        }

        [Test]
        public void Parse_MissingAction_IsDeny()
        {
            Assert.IsTrue(_parser.TryParse("{\"reply\":\"Ne istiyorsun?\"}", out NpcDecision d));
            Assert.AreEqual(NpcAction.Deny, d.Action);
        }

        [TestCase(" Allow ")]
        [TestCase("ALLOW")]
        [TestCase("allow\n")]
        public void Parse_ActionIsCaseAndSpaceInsensitive(string action)
        {
            string json = "{\"action\":\"" + action.Replace("\n", "\\n") + "\",\"reply\":\"Geç.\"}";

            Assert.IsTrue(_parser.TryParse(json, out NpcDecision d));
            Assert.AreEqual(NpcAction.Allow, d.Action);
        }

        // Sözsüz "allow" ile kapı açılmamalı.
        [TestCase("{\"action\":\"allow\",\"reply\":\"\"}")]
        [TestCase("{\"action\":\"allow\",\"reply\":\"   \"}")]
        [TestCase("{\"action\":\"allow\"}")]
        public void Parse_EmptyReply_Fails(string json)
        {
            Assert.IsFalse(_parser.TryParse(json, out _));
        }

        [Test]
        public void Parse_BrokenJson_Fails()
        {
            Assert.IsFalse(_parser.TryParse("{\"action\":\"allow\"", out _));
        }

        [Test]
        public void Parse_FencedJson_Fails()
        {
            Assert.IsFalse(_parser.TryParse("```json\n{\"action\":\"allow\",\"reply\":\"Geç.\"}\n```", out _));
        }

        [TestCase("Kapıyı açıyorum")]
        [TestCase("")]
        [TestCase(null)]
        public void Parse_PlainText_Fails(string content)
        {
            Assert.IsFalse(_parser.TryParse(content, out _));
        }

        [Test]
        public void Parse_LongReply_IsTruncated()
        {
            string json = "{\"action\":\"deny\",\"reply\":\"" + new string('a', 500) + "\"}";

            Assert.IsTrue(_parser.TryParse(json, out NpcDecision d));
            Assert.AreEqual(MaxReply, d.Reply.Length);
        }

        // Model fazladan alan eklese bile yalnızca action ve reply okunur.
        [Test]
        public void Parse_ExtraFieldsIgnored()
        {
            string json = "{\"action\":\"deny\",\"reply\":\"Hayır.\",\"code\":\"System.IO.File.Delete(\\\"x\\\")\"}";

            Assert.IsTrue(_parser.TryParse(json, out NpcDecision d));
            Assert.AreEqual(NpcAction.Deny, d.Action);
            Assert.AreEqual("Hayır.", d.Reply);
        }
    }
}
