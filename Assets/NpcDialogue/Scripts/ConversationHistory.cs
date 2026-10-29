using System.Collections.Generic;
using UnityEngine;

namespace YazLab.NpcDialogue
{
    // Modele gidecek mesaj listesini kurar: [kurallar, ...son N tur, oyuncu, hatırlatma].
    public class ConversationHistory
    {
        private readonly string _systemPrompt;
        private readonly string _reminder;
        private readonly int _maxTurns;
        private readonly List<ChatMessage> _turns = new List<ChatMessage>(); // user, assistant çiftleri

        public ConversationHistory(string systemPrompt, string reminder, int maxTurns)
        {
            _systemPrompt = systemPrompt;
            _reminder = reminder;
            _maxTurns = maxTurns;
        }

        public int TurnCount => _turns.Count / 2;

        // extraSystem: yalnızca bu tura eklenen ek kural (ör. sabır tükendi); geçmişe girmez.
        public IReadOnlyList<ChatMessage> BuildMessages(string userText, string extraSystem = null)
        {
            var messages = new List<ChatMessage>(_turns.Count + 4) { Message("system", _systemPrompt) };
            messages.AddRange(_turns);
            messages.Add(Message("user", userText));
            // Hatırlatma en sonda: oyuncunun metni kuralların son sözü olamaz. Geçmişe girmez.
            if (!string.IsNullOrWhiteSpace(_reminder))
                messages.Add(Message("system", _reminder));
            if (!string.IsNullOrWhiteSpace(extraSystem))
                messages.Add(Message("system", extraSystem));
            return messages;
        }

        // Yalnızca başarılı turlar eklenir. Modelin ham metni değil, ayrıştırılmış temiz JSON saklanır.
        public void AddTurn(string userText, NpcDecision decision)
        {
            var clean = new ReplyJson { action = decision.Action.ToString().ToLowerInvariant(), reply = decision.Reply };
            _turns.Add(Message("user", userText));
            _turns.Add(Message("assistant", JsonUtility.ToJson(clean)));

            // Bağlam (num_ctx) taşmasın diye en eski tur düşer.
            while (_turns.Count > _maxTurns * 2)
                _turns.RemoveRange(0, 2);
        }

        private static ChatMessage Message(string role, string content) =>
            new ChatMessage { role = role, content = content };
    }
}
