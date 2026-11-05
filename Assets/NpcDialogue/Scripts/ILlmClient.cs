using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YazLab.NpcDialogue
{
    public interface ILlmClient
    {
        Task<LlmResult> SendAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct);
    }
}
