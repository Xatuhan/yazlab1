using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YazLab.NpcDialogue
{
    // Yerel Ollama'ya async istek atar; her hatayı LlmError'a çevirir, oyuna exception sızdırmaz.
    public sealed class OllamaClient : ILlmClient, IDisposable
    {
        private const int MaxResponseBytes = 64 * 1024; // beklenmedik dev cevap belleği şişirmesin

        private readonly OllamaSettings _settings;
        private readonly Uri _chatUri;
        private readonly HttpClient _http;

        public OllamaClient(OllamaSettings settings)
        {
            _settings = settings;
            _chatUri = new Uri(new Uri(settings.baseUrl), "/api/chat");
            // Süreyi kendi iptal belirtecimiz yönetir; böylece zaman aşımını dış iptalden ayırabiliriz.
            _http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        }

        public async Task<LlmResult> SendAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(_settings.timeoutSeconds));
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, _chatUri)
                    {
                        Content = new StringContent(BuildRequestBody(_settings, messages), Encoding.UTF8, "application/json")
                    };
                    // ConfigureAwait yok: devam Unity ana iş parçacığında olur, arayüz güvenle güncellenir.
                    using (HttpResponseMessage response = await _http.SendAsync(
                               request, HttpCompletionOption.ResponseHeadersRead, timeout.Token))
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                            return LlmResult.Fail(LlmError.ModelNotFound);
                        if (!response.IsSuccessStatusCode)
                            return LlmResult.Fail(LlmError.BadResponse);

                        string body = await ReadLimitedAsync(response.Content, timeout.Token);
                        string content = body == null ? null : JsonUtility.FromJson<OllamaChatResponse>(body)?.message?.content;
                        return string.IsNullOrWhiteSpace(content)
                            ? LlmResult.Fail(LlmError.BadResponse)
                            : LlmResult.Success(content);
                    }
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    return LlmResult.Fail(LlmError.Timeout); // bizim süremiz doldu
                }
                catch (OperationCanceledException)
                {
                    throw; // oyuncu sahneden çıktı: çağıran sessizce bırakır
                }
                catch (Exception e) when (e is HttpRequestException || e is IOException)
                {
                    return LlmResult.Fail(LlmError.ServerUnreachable); // Ollama kapalı ya da bağlantı koptu
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Ollama cevabı işlenemedi: " + e.GetType().Name);
                    return LlmResult.Fail(LlmError.BadResponse); // bozuk JSON vb.
                }
            }
        }

        // Gövde JsonUtility ile üretilir: oyuncu tırnak ya da satır sonu yazsa da JSON bozulmaz.
        internal static string BuildRequestBody(OllamaSettings s, IReadOnlyList<ChatMessage> messages)
        {
            var request = new OllamaChatRequest
            {
                model = s.model,
                messages = messages.ToArray(),
                stream = false,
                think = false, // gemma4'te düşünme varsayılan açık; kapalı olmazsa cevap gecikir
                keep_alive = s.keepAlive,
                options = new OllamaOptions { num_ctx = s.numCtx, num_predict = s.numPredict, temperature = s.temperature }
            };
            return JsonUtility.ToJson(request);
        }

        // En çok MaxResponseBytes okur; fazlası gelirse null döner.
        private static async Task<string> ReadLimitedAsync(HttpContent content, CancellationToken ct)
        {
            using (Stream stream = await content.ReadAsStreamAsync())
            using (var buffer = new MemoryStream())
            {
                var chunk = new byte[8192];
                int read;
                while ((read = await stream.ReadAsync(chunk, 0, chunk.Length, ct)) > 0)
                {
                    if (buffer.Length + read > MaxResponseBytes)
                        return null;
                    buffer.Write(chunk, 0, read);
                }
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        public void Dispose() => _http.Dispose();
    }
}
