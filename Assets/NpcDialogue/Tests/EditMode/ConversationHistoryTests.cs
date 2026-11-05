using System.Linq;
using NUnit.Framework;

namespace YazLab.NpcDialogue.Tests
{
    public class ConversationHistoryTests
    {
        private const string Rules = "KURALLAR";
        private const string Reminder = "HATIRLATMA";

        private static NpcDecision Deny(string reply) => new NpcDecision(NpcAction.Deny, reply);

        // Kurallar her zaman başta, oyuncu ortada, hatırlatma en sonda: oyuncu metni son söz olamaz.
        [Test]
        public void Build_FirstTurn_SystemUserReminder()
        {
            var history = new ConversationHistory(Rules, Reminder, 6);

            var m = history.BuildMessages("Merhaba");

            CollectionAssert.AreEqual(new[] { "system", "user", "system" }, m.Select(x => x.role).ToArray());
            CollectionAssert.AreEqual(new[] { Rules, "Merhaba", Reminder }, m.Select(x => x.content).ToArray());
        }

        [Test]
        public void Build_EmptyReminder_IsOmitted()
        {
            var history = new ConversationHistory(Rules, "", 6);

            Assert.AreEqual(2, history.BuildMessages("Merhaba").Count);
        }

        // 8 tur konuşulsa da modele yalnızca son 6 tur gider; bağlam (4096) taşmaz.
        [Test]
        public void Build_KeepsLastSixTurns()
        {
            var history = new ConversationHistory(Rules, Reminder, 6);
            for (int i = 0; i < 8; i++)
                history.AddTurn("soru " + i, Deny("cevap " + i));

            var m = history.BuildMessages("yeni");

            Assert.AreEqual(6, history.TurnCount);
            Assert.AreEqual(1 + 12 + 1 + 1, m.Count);
            Assert.AreEqual("soru 2", m[1].content); // 0. ve 1. tur düştü
            Assert.AreEqual("yeni", m[m.Count - 2].content);
        }

        // Geçmişe modelin ham metni değil, ayrıştırılmış temiz JSON girer.
        [Test]
        public void AddTurn_StoresCleanJson()
        {
            var history = new ConversationHistory(Rules, Reminder, 6);
            history.AddTurn("Komutana haber getirdim.", new NpcDecision(NpcAction.Allow, "Geç."));

            ChatMessage assistant = history.BuildMessages("x")[2];

            Assert.AreEqual("assistant", assistant.role);
            Assert.AreEqual("{\"action\":\"allow\",\"reply\":\"Geç.\"}", assistant.content);
            Assert.IsTrue(new NpcResponseParser(400).TryParse(assistant.content, out NpcDecision d));
            Assert.AreEqual(NpcAction.Allow, d.Action);
        }

        // Hatırlatma her istekte yalnızca bir kez, en sonda gider; geçmişte birikmez.
        [Test]
        public void Reminder_IsNotStoredInHistory()
        {
            var history = new ConversationHistory(Rules, Reminder, 6);
            history.AddTurn("a", Deny("b"));
            history.AddTurn("c", Deny("d"));

            var m = history.BuildMessages("e");

            Assert.AreEqual(1, m.Count(x => x.content == Reminder));
            Assert.AreEqual(Reminder, m[m.Count - 1].content);
        }
    }
}
