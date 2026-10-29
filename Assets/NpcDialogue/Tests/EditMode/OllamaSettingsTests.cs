using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace YazLab.NpcDialogue.Tests
{
    public class OllamaSettingsTests
    {
        private const string ValidJson =
            "{\"baseUrl\":\"http://127.0.0.1:11434\",\"model\":\"gemma4:e2b\",\"timeoutSeconds\":60," +
            "\"numCtx\":4096,\"numPredict\":200,\"temperature\":0.5,\"keepAlive\":\"30m\"," +
            "\"maxInputChars\":200,\"maxReplyChars\":400,\"historyTurns\":6,\"patienceTurns\":8}";

        private static string StreamingFile(string name) =>
            Path.Combine(Application.streamingAssetsPath, "NpcDialogue", name);

        // Oyunla birlikte giden gerçek ayar dosyasının geçerli olduğunu kanıtlar.
        [Test]
        public void TryParse_ShippedFile_IsValid()
        {
            string json = File.ReadAllText(StreamingFile("ollama_settings.json"));

            Assert.IsTrue(OllamaSettings.TryParse(json, out OllamaSettings s, out string error), error);
            Assert.AreEqual("http://127.0.0.1:11434", s.baseUrl);
            Assert.AreEqual("gemma4:e2b", s.model);
            Assert.AreEqual(4096, s.numCtx);
            Assert.AreEqual(200, s.numPredict);
            Assert.AreEqual(0.5f, s.temperature);
            Assert.AreEqual(6, s.historyTurns);
            Assert.AreEqual(8, s.patienceTurns);
        }

        [TestCase("guard_prompt.txt")]
        [TestCase("guard_reminder.txt")]
        [TestCase("guard_patience.txt")]
        [TestCase("intro.txt")]
        public void PromptFile_ExistsAndNotEmpty(string name)
        {
            string prompt = File.ReadAllText(StreamingFile(name));

            Assert.IsFalse(string.IsNullOrWhiteSpace(prompt));
        }

        // Oyuncu yazısının bu makine dışına gitmediğini kanıtlar.
        [TestCase("http://192.168.1.5:11434")]
        [TestCase("http://localhost:11434")]
        [TestCase("http://localhost.evil.com:11434")]
        [TestCase("http://127.0.0.1.evil.com:11434")]
        [TestCase("http://127.0.0.1@evil.com:11434")]
        [TestCase("https://127.0.0.1:11434")]
        public void TryParse_NonLocalHost_Fails(string url)
        {
            string json = ValidJson.Replace("http://127.0.0.1:11434", url);

            Assert.IsFalse(OllamaSettings.TryParse(json, out OllamaSettings s, out string error));
            Assert.IsNull(s);
            Assert.IsNotEmpty(error);
        }

        [TestCase("{\"baseUrl\":\"http://127.0.0.1:11434\",")]
        [TestCase("")]
        public void TryParse_BrokenJson_Fails(string json)
        {
            Assert.IsFalse(OllamaSettings.TryParse(json, out _, out string error));
            Assert.IsNotEmpty(error);
        }

        [TestCase("\"timeoutSeconds\":60", "\"timeoutSeconds\":0")]
        [TestCase("\"numCtx\":4096", "\"numCtx\":-1")]
        [TestCase("\"maxInputChars\":200", "\"maxInputChars\":0")]
        [TestCase("\"historyTurns\":6", "\"historyTurns\":0")]
        [TestCase("\"numPredict\":200", "\"numPredict\":0")]
        [TestCase("\"temperature\":0.5", "\"temperature\":0")]
        [TestCase("\"temperature\":0.5", "\"temperature\":3")]
        [TestCase(",\"temperature\":0.5", "")]
        [TestCase("\"patienceTurns\":8", "\"patienceTurns\":0")]
        public void TryParse_InvalidNumbers_Fail(string from, string to)
        {
            Assert.IsFalse(OllamaSettings.TryParse(ValidJson.Replace(from, to), out _, out _));
        }

        [Test]
        public void TryParse_EmptyModel_Fails()
        {
            string json = ValidJson.Replace("\"model\":\"gemma4:e2b\"", "\"model\":\"\"");

            Assert.IsFalse(OllamaSettings.TryParse(json, out _, out _));
        }
    }
}
