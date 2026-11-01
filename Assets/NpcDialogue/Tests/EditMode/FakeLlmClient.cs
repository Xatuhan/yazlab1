using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YazLab.NpcDialogue.Tests
{
    // Yalnızca test: modele gitmeden sabit sonuç döndüren istemci. Oyun koduna ve build'e girmez
    // (Editor-only test assembly). Gerçek model cevabının yerine geçmez; tur mantığını sınamak içindir.
    internal sealed class FakeLlmClient : ILlmClient
    {
        public LlmResult Next = LlmResult.Success("{\"action\":\"deny\",\"reply\":\"Hayır.\"}");
        public TaskCompletionSource<LlmResult> Gate; // doluysa cevap bu tamamlanana kadar bekler
        public bool Cancel;                          // true ise istek iptal edilmiş gibi davranır

        public int Calls { get; private set; }
        public IReadOnlyList<ChatMessage> LastMessages { get; private set; }

        public Task<LlmResult> SendAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct)
        {
            Calls++;
            LastMessages = messages;
            if (Cancel)
                return Task.FromCanceled<LlmResult>(new CancellationToken(true));
            return Gate != null ? Gate.Task : Task.FromResult(Next);
        }
    }
}
