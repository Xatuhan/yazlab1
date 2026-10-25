using System.Collections.Generic;
using NUnit.Framework;

namespace YazLab.NpcDialogue.Tests
{
    // Her hata oyuncuya kendi Türkçe mesajıyla gider; İngilizce hata adı ekrana sızmaz (PDF 2.9 / 4.3).
    public class SystemMessagesTests
    {
        private static readonly LlmError[] Errors =
            { LlmError.ServerUnreachable, LlmError.ModelNotFound, LlmError.Timeout, LlmError.BadResponse };

        [Test]
        public void EveryError_HasOwnTurkishMessage_WithoutEnumName()
        {
            var seen = new HashSet<string>();
            foreach (LlmError error in Errors)
            {
                string message = SystemMessages.For(error, "gemma4:e2b", 60);

                Assert.IsFalse(string.IsNullOrWhiteSpace(message), error.ToString());
                StringAssert.DoesNotContain(error.ToString(), message);
                Assert.AreNotEqual(SystemMessages.Unexpected, message, error.ToString());
                Assert.IsTrue(seen.Add(message), "aynı mesaj iki hataya verilmiş: " + error);
            }
        }

        // Oyuncu ne yapacağını bilsin: indirilecek model adı ve beklenen süre mesajda yazar.
        [Test]
        public void ModelNotFound_NamesModelAndPullCommand()
        {
            string message = SystemMessages.For(LlmError.ModelNotFound, "gemma4:e2b", 60);

            StringAssert.Contains("'gemma4:e2b'", message);
            StringAssert.Contains("ollama pull gemma4:e2b", message);
        }

        [Test]
        public void Timeout_ShowsSeconds()
        {
            StringAssert.Contains("60 saniyede", SystemMessages.For(LlmError.Timeout, "gemma4:e2b", 60));
        }

        [Test]
        public void ServerUnreachable_TellsToOpenOllama()
        {
            StringAssert.Contains("Ollama", SystemMessages.For(LlmError.ServerUnreachable, "gemma4:e2b", 60));
        }
    }
}
