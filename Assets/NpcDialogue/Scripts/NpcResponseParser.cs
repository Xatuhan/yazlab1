using System;
using UnityEngine;

namespace YazLab.NpcDialogue
{
    // Modelin JSON metnini NpcDecision'a çevirir. Yalnızca action ve reply okunur, metin asla çalıştırılmaz.
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

            // ReplyJson'da yalnızca action ve reply var; modelin eklediği fazla alanlar yok sayılır.
            ReplyJson raw;
            try
            {
                raw = JsonUtility.FromJson<ReplyJson>(content);
            }
            catch (Exception)
            {
                return false; // JSON değil ya da yarım kalmış
            }

            // Sözsüz karar kabul edilmez: reply boşsa kapı da açılmaz.
            if (raw == null || string.IsNullOrWhiteSpace(raw.reply))
                return false;

            decision = new NpcDecision(ToAction(raw.action), Truncate(raw.reply.Trim()));
            return true;
        }

        // Bilinen üç değer dışındaki her şey (eksik, yanlış yazılmış, başka dilde) reddetmedir.
        private static NpcAction ToAction(string action)
        {
            switch (action?.Trim().ToLowerInvariant())
            {
                case "allow": return NpcAction.Allow;
                case "attack": return NpcAction.Attack;
                default: return NpcAction.Deny;
            }
        }

        private string Truncate(string reply)
        {
            if (reply.Length <= _maxReplyChars)
                return reply;

            int length = _maxReplyChars;
            // Emoji gibi iki parçalı karakter ortadan bölünmesin.
            if (char.IsHighSurrogate(reply[length - 1]))
                length--;
            return reply.Substring(0, length);
        }
    }
}
