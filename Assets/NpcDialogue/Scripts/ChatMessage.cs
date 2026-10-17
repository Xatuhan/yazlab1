using System;

namespace YazLab.NpcDialogue
{
    // Ollama /api/chat mesajı. Alan adları JSON anahtarlarıyla aynı olmalı (JsonUtility).
    [Serializable]
    public class ChatMessage
    {
        public string role;    // "system", "user" ya da "assistant"
        public string content;
    }
}
