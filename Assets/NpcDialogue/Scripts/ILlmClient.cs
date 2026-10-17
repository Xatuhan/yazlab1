using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YazLab.NpcDialogue
{
    // Dil modeline mesaj gönderen istemci. Ayrı arayüz: NPC mantığı Ollama'ya bağımlı olmaz,
    // testte sahte istemci takılabilir.
    public interface ILlmClient
    {
        // Hatalar exception olarak değil LlmResult.Error olarak döner; yalnızca dış iptal exception atar.
        Task<LlmResult> SendAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct);
    }
}
