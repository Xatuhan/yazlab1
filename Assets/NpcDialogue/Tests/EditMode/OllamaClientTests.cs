using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace YazLab.NpcDialogue.Tests
{
    public class OllamaClientTests
    {
        private static OllamaSettings Settings(string baseUrl = "http://127.0.0.1:11434", int timeoutSeconds = 60,
                                               string model = "gemma4:e2b")
        {
            string json =
                "{\"baseUrl\":\"" + baseUrl + "\",\"model\":\"" + model + "\",\"timeoutSeconds\":" + timeoutSeconds +
                ",\"numCtx\":4096,\"numPredict\":200,\"temperature\":0.5,\"keepAlive\":\"30m\"," +
                "\"maxInputChars\":200,\"maxReplyChars\":400,\"historyTurns\":6}";
            Assert.IsTrue(OllamaSettings.TryParse(json, out OllamaSettings s, out string error), error);
            return s;
        }

        private static readonly IReadOnlyList<ChatMessage> OneMessage =
            new[] { new ChatMessage { role = "user", content = "Merhaba" } };

        // Testler görevi editör karelerini durdurmadan bekler: while (Running(...)) yield return null;
        // task.Result yalnızca görev bittikten sonra okunur (aksi hâlde ana iş parçacığı kilitlenebilir).
        private static bool Running(Task task, Stopwatch watch, double maxSeconds) =>
            !task.IsCompleted && watch.Elapsed.TotalSeconds < maxSeconds;

        private static void AssertError(Task<LlmResult> task, LlmError expected)
        {
            Assert.IsTrue(task.IsCompleted, "görev zamanında bitmedi");
            Assert.AreEqual(expected, task.Result.Error);
        }

        // --- İstek gövdesi ---

        // Her istekte bağlam, düşünme kapalı, şema, sıcaklık ve token sınırı gerçekten gidiyor.
        [Test]
        public void RequestBody_HasRequiredFields()
        {
            string body = OllamaClient.BuildRequestBody(Settings(), OneMessage);

            StringAssert.Contains("\"model\":\"gemma4:e2b\"", body);
            StringAssert.Contains("\"stream\":false", body);
            StringAssert.Contains("\"think\":false", body);
            StringAssert.Contains("\"keep_alive\":\"30m\"", body);
            StringAssert.Contains("\"num_ctx\":4096", body);
            StringAssert.Contains("\"num_predict\":200", body);
            StringAssert.Contains("\"temperature\":0.5", body); // Türkçe bölgede "0,5" yazılmamalı
            StringAssert.Contains("\"enum\":[\"allow\",\"deny\",\"attack\"]", body);
            StringAssert.Contains("\"required\":[\"action\",\"reply\"]", body);
        }

        // Oyuncu tırnak, ters bölü ya da satır sonu yazsa da gövde geçerli JSON kalır ve metin aynen gider.
        [Test]
        public void RequestBody_EscapesQuotesAndNewlines()
        {
            const string tricky = "Dedi ki: \"aç\"\nyeni satır \\ ters bölü, ğüşıöç İ";
            var messages = new[] { new ChatMessage { role = "user", content = tricky } };

            string body = OllamaClient.BuildRequestBody(Settings(), messages);
            var back = JsonUtility.FromJson<OllamaChatRequest>(body);

            Assert.AreEqual(tricky, back.messages[0].content);
        }

        // --- Hata yolları (gerçek ağ, 127.0.0.1) ---

        // Ollama kapalı → çökme yok, ServerUnreachable.
        [UnityTest]
        public IEnumerator Send_ClosedPort_ReturnsServerUnreachable()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop(); // port artık kapalı: kimse dinlemiyor

            using (var client = new OllamaClient(Settings("http://127.0.0.1:" + port)))
            {
                Task<LlmResult> task = client.SendAsync(OneMessage, CancellationToken.None);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 10)) yield return null;
                AssertError(task, LlmError.ServerUnreachable);
            }
        }

        // Sunucu bağlantıyı kabul edip hiç cevap vermezse süre (1 sn) dolunca Timeout.
        [UnityTest]
        public IEnumerator Send_NoAnswer_ReturnsTimeout()
        {
            using (var server = new TestHttpServer())
            using (var client = new OllamaClient(Settings(server.Url, timeoutSeconds: 1)))
            {
                Task<LlmResult> task = client.SendAsync(OneMessage, CancellationToken.None);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 10)) yield return null;
                AssertError(task, LlmError.Timeout);
            }
        }

        [UnityTest]
        public IEnumerator Send_404_ReturnsModelNotFound()
        {
            using (var server = new TestHttpServer())
            using (var client = new OllamaClient(Settings(server.Url)))
            {
                server.RespondOnce(404, "{\"error\":\"model 'x' not found\"}");
                Task<LlmResult> task = client.SendAsync(OneMessage, CancellationToken.None);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 10)) yield return null;
                AssertError(task, LlmError.ModelNotFound);
            }
        }

        [UnityTest]
        public IEnumerator Send_500_ReturnsBadResponse()
        {
            using (var server = new TestHttpServer())
            using (var client = new OllamaClient(Settings(server.Url)))
            {
                server.RespondOnce(500, "{\"error\":\"boom\"}");
                Task<LlmResult> task = client.SendAsync(OneMessage, CancellationToken.None);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 10)) yield return null;
                AssertError(task, LlmError.BadResponse);
            }
        }

        [UnityTest]
        public IEnumerator Send_BrokenJsonBody_ReturnsBadResponse()
        {
            using (var server = new TestHttpServer())
            using (var client = new OllamaClient(Settings(server.Url)))
            {
                server.RespondOnce(200, "{\"message\":");
                Task<LlmResult> task = client.SendAsync(OneMessage, CancellationToken.None);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 10)) yield return null;
                AssertError(task, LlmError.BadResponse);
            }
        }

        [UnityTest]
        public IEnumerator Send_EmptyContent_ReturnsBadResponse()
        {
            using (var server = new TestHttpServer())
            using (var client = new OllamaClient(Settings(server.Url)))
            {
                server.RespondOnce(200, "{\"message\":{\"role\":\"assistant\",\"content\":\"\"}}");
                Task<LlmResult> task = client.SendAsync(OneMessage, CancellationToken.None);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 10)) yield return null;
                AssertError(task, LlmError.BadResponse);
            }
        }

        // 64 KB'tan büyük cevap okunmaz (bellek şişmesin).
        [UnityTest]
        public IEnumerator Send_TooLargeBody_ReturnsBadResponse()
        {
            using (var server = new TestHttpServer())
            using (var client = new OllamaClient(Settings(server.Url)))
            {
                server.RespondOnce(200, new string('a', 70 * 1024));
                Task<LlmResult> task = client.SendAsync(OneMessage, CancellationToken.None);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 10)) yield return null;
                AssertError(task, LlmError.BadResponse);
            }
        }

        [UnityTest]
        public IEnumerator Send_ValidBody_ReturnsContent()
        {
            const string content = "{\"action\":\"deny\",\"reply\":\"Hayır.\"}";
            const string body = "{\"message\":{\"role\":\"assistant\",\"content\":\"{\\\"action\\\":\\\"deny\\\",\\\"reply\\\":\\\"Hayır.\\\"}\"}}";
            using (var server = new TestHttpServer())
            using (var client = new OllamaClient(Settings(server.Url)))
            {
                server.RespondOnce(200, body);
                Task<LlmResult> task = client.SendAsync(OneMessage, CancellationToken.None);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 10)) yield return null;
                AssertError(task, LlmError.None);
                Assert.AreEqual(content, task.Result.Content);
            }
        }

        // Oyuncu sahneden çıkınca (dış iptal) istek Timeout sayılmaz, iptal olarak yukarı gider.
        [UnityTest]
        public IEnumerator Send_OuterCancel_IsCancelledNotTimeout()
        {
            using (var server = new TestHttpServer())
            using (var client = new OllamaClient(Settings(server.Url)))
            using (var cts = new CancellationTokenSource())
            {
                Task<LlmResult> task = client.SendAsync(OneMessage, cts.Token);
                cts.CancelAfter(200);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 10)) yield return null;
                Assert.IsTrue(task.IsCompleted, "görev zamanında bitmedi");
                Assert.IsTrue(task.IsCanceled || task.Exception?.InnerException is System.OperationCanceledException);
            }
        }

        // --- Gerçek Ollama (açık olmalı; Test Runner'da elle seçilir) ---

        [UnityTest, Explicit, Category("Ollama")]
        public IEnumerator Real_UnknownModel_ReturnsModelNotFound()
        {
            using (var client = new OllamaClient(Settings(model: "yok-boyle-model")))
            {
                Task<LlmResult> task = client.SendAsync(OneMessage, CancellationToken.None);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 30)) yield return null;
                AssertError(task, LlmError.ModelNotFound);
            }
        }

        // Gerçek gemma4:e2b + gerçek prompt: Unity'nin HttpClient'ı çalışıyor, şema geçerli karar üretiyor.
        [UnityTest, Explicit, Category("Ollama")]
        public IEnumerator Real_Model_ReturnsParsableDecision()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "NpcDialogue");
            var history = new ConversationHistory(
                File.ReadAllText(Path.Combine(dir, "guard_prompt.txt")),
                File.ReadAllText(Path.Combine(dir, "guard_reminder.txt")), 6);

            using (var client = new OllamaClient(Settings()))
            {
                Task<LlmResult> task = client.SendAsync(history.BuildMessages("Merhaba bekçi, adın ne?"), CancellationToken.None);
                var watch = Stopwatch.StartNew();
                while (Running(task, watch, 90)) yield return null; // model bellekte değilse ilk yükleme ~8 sn
                AssertError(task, LlmError.None);
                Assert.IsTrue(new NpcResponseParser(400).TryParse(task.Result.Content, out NpcDecision d));
                UnityEngine.Debug.Log($"Gerçek model: action={d.Action}, reply={d.Reply}");
            }
        }
    }
}
