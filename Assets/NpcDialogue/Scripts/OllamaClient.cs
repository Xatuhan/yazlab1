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
    public sealed class OllamaClient : ILlmClient, IDisposable
    {
        private const int MaxResponseBytes = 64 * 1024; // dev cevap belleği şişirmesin

        private readonly OllamaSettings _settings;
        private readonly Uri _chatUri;
        private readonly HttpClient _http;

        public OllamaClient(OllamaSettings settings)
        {
            _settings = settings;
            _chatUri = new Uri(new Uri(settings.baseUrl), "/api/chat");
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
                    return LlmResult.Fail(LlmError.Timeout);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e) when (e is HttpRequestException || e is IOException)
                {
                    return LlmResult.Fail(LlmError.ServerUnreachable);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Ollama cevabı işlenemedi: " + e.GetType().Name);
                    return LlmResult.Fail(LlmError.BadResponse);
                }
            }
        }

        internal static string BuildRequestBody(OllamaSettings s, IReadOnlyList<ChatMessage> messages)
        {
            var request = new OllamaChatRequest
            {
                model = s.model,
                messages = messages.ToArray(),
                stream = false,
                think = false, // açıkken cevap gecikiyor
                keep_alive = s.keepAlive,
                options = new OllamaOptions { num_ctx = s.numCtx, num_predict = s.numPredict, temperature = s.temperature }
            };
            return JsonUtility.ToJson(request);
        }

        private static async Task<string> ReadLimitedAsync(HttpContent content, CancellationToken ct)
        {
            // her parçada ana iş parçacığına dönmesin
            using (Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var buffer = new MemoryStream())
            {
                var chunk = new byte[8192];
                int read;
                while ((read = await stream.ReadAsync(chunk, 0, chunk.Length, ct).ConfigureAwait(false)) > 0)
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
