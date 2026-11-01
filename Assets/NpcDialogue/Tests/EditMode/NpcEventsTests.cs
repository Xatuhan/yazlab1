using NUnit.Framework;

namespace YazLab.NpcDialogue.Tests
{
    // Bekçinin kararının doğru oyun olayına dönüştüğünü kanıtlar (PDF 3.2: "kapının açılması, savaşın tetiklenmesi";
    // PDF 6: LLM çıktısının oyun durumuna hatasız yansıması).
    public class NpcEventsTests
    {
        private int _doorUnlocked;
        private int _playerDefeated;
        private string _reply;
        private int _turns;

        private void OnDoor(string reply, int turns) { _doorUnlocked++; _reply = reply; _turns = turns; }
        private void OnDefeat(string reply, int turns) { _playerDefeated++; _reply = reply; _turns = turns; }

        [SetUp]
        public void SetUp()
        {
            _doorUnlocked = 0;
            _playerDefeated = 0;
            _reply = null;
            _turns = 0;
            NpcEvents.DoorUnlocked += OnDoor;
            NpcEvents.PlayerDefeated += OnDefeat;
        }

        [TearDown]
        public void TearDown()
        {
            NpcEvents.DoorUnlocked -= OnDoor;
            NpcEvents.PlayerDefeated -= OnDefeat;
        }

        [Test]
        public void Allow_UnlocksDoor_WithReplyAndTurns_AndEndsGame()
        {
            Assert.IsTrue(NpcEvents.Publish(new NpcDecision(NpcAction.Allow, "Geç."), 3));
            Assert.AreEqual(1, _doorUnlocked);
            Assert.AreEqual(0, _playerDefeated);
            Assert.AreEqual("Geç.", _reply);
            Assert.AreEqual(3, _turns);
        }

        [Test]
        public void Attack_DefeatsPlayer_WithReplyAndTurns_AndEndsGame()
        {
            Assert.IsTrue(NpcEvents.Publish(new NpcDecision(NpcAction.Attack, "Al bakalım!"), 2));
            Assert.AreEqual(0, _doorUnlocked);
            Assert.AreEqual(1, _playerDefeated);
            Assert.AreEqual("Al bakalım!", _reply);
            Assert.AreEqual(2, _turns);
        }

        // Reddetme oyunu bitirmez, hiçbir olay yayınlanmaz: kapı kilitli, konuşma sürer.
        [Test]
        public void Deny_PublishesNothing_AndConversationContinues()
        {
            Assert.IsFalse(NpcEvents.Publish(new NpcDecision(NpcAction.Deny, "Olmaz."), 1));
            Assert.AreEqual(0, _doorUnlocked);
            Assert.AreEqual(0, _playerDefeated);
        }

        // Dinleyici yokken yayın çökmez (ör. sahnede kapı ya da bitiş ekranı yok).
        [Test]
        public void Publish_WithoutListeners_DoesNotThrow()
        {
            TearDown();
            Assert.DoesNotThrow(() => NpcEvents.Publish(new NpcDecision(NpcAction.Allow, "a"), 1));
            Assert.DoesNotThrow(() => NpcEvents.Publish(new NpcDecision(NpcAction.Attack, "b"), 1));
        }
    }
}
