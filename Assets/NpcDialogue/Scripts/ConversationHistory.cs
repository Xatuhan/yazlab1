using System.Collections.Generic;
using UnityEngine;

namespace YazLab.NpcDialogue
{
    public class ConversationHistory
    {
        private readonly string _systemPrompt;
        private readonly string _reminder;
        private readonly int _maxTurns;
        private readonly List<ChatMessage> _turns = new List<ChatMessage>();

        public ConversationHistory(string systemPrompt, string reminder, int maxTurns)
        {
            _systemPrompt = systemPrompt;
            _reminder = reminder;
            _maxTurns = maxTurns;
        }

        public int TurnCount => _turns.Count / 2;

        public IReadOnlyList<ChatMessage> BuildMessages(string userText, string extraSystem = null)
        {
            var messages = new List<ChatMessage>(_turns.Count + 4) { Message("system", _systemPrompt) };
            messages.AddRange(_turns);
            messages.Add(Message("user", userText));
            if (!string.IsNullOrWhiteSpace(_reminder))
                messages.Add(Message("system", _reminder)); // hatırlatma hep en sonda
            if (!string.IsNullOrWhiteSpace(extraSystem))
                messages.Add(Message("system", extraSystem));
            return messages;
        }

        public void AddTurn(string userText, NpcDecision decision)
        {
            var clean = new ReplyJson { action = decision.Action.ToString().ToLowerInvariant(), reply = decision.Reply };
            _turns.Add(Message("user", userText));
            _turns.Add(Message("assistant", JsonUtility.ToJson(clean)));

            while (_turns.Count > _maxTurns * 2) // bağlam taşmasın
                _turns.RemoveRange(0, 2);
        }

        private static ChatMessage Message(string role, string content) =>
            new ChatMessage { role = role, content = content };
    }
}
