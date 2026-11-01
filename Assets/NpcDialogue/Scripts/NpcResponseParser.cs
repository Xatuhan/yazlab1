using System;
using UnityEngine;

namespace YazLab.NpcDialogue
{
    public class NpcResponseParser : IResponseParser
    {
        private readonly int _maxReplyChars;

        public NpcResponseParser(int maxReplyChars)
        {
            _maxReplyChars = maxReplyChars;
        }

        public bool TryParse(string content, out NpcDecision decision)
        {
            decision = default;
            if (string.IsNullOrWhiteSpace(content))
                return false;

            ReplyJson raw;
            try
            {
                raw = JsonUtility.FromJson<ReplyJson>(content);
            }
            catch (Exception)
            {
                return false;
            }

            if (raw == null || string.IsNullOrWhiteSpace(raw.reply))
                return false;

            decision = new NpcDecision(ToAction(raw.action), Truncate(raw.reply.Trim()));
            return true;
        }

        private static NpcAction ToAction(string action)
        {
            switch (action?.Trim().ToLowerInvariant())
            {
                case "allow": return NpcAction.Allow;
                case "attack": return NpcAction.Attack;
                default: return NpcAction.Deny; // bilinmeyen karar = deny
            }
        }

        private string Truncate(string reply)
        {
            if (reply.Length <= _maxReplyChars)
                return reply;

            int length = _maxReplyChars;
            if (char.IsHighSurrogate(reply[length - 1]))
                length--;
            return reply.Substring(0, length);
        }
    }
}
