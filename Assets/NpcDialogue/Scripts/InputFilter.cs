using System.Collections.Generic;
using System.Text;

namespace YazLab.NpcDialogue
{
    public static class InputFilter
    {
        private static readonly HashSet<char> Symbols = new HashSet<char> { '{', '}', '<', '>', '[', ']' };

        private static readonly HashSet<string> Words = new HashSet<string>
        {
            "allow", "deny", "attack", "action", "json", "system", "sistem", "reply", "prompt"
        };

        private static readonly string[] Glued = { "allow", "attack", "action", "json" };

        public static bool IsBlocked(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var word = new StringBuilder();
            var glued = new StringBuilder();
            foreach (char c in Normalize(text))
            {
                if (Symbols.Contains(c))
                    return true;
                if (char.IsLetter(c))
                {
                    if (c > 'ɏ')
                        return true;
                    word.Append(c);
                    glued.Append(c);
                }
                else if (EndWord(word))
                {
                    return true;
                }
            }
            if (EndWord(word))
                return true;

            string all = glued.ToString();
            foreach (string g in Glued)
                if (all.Contains(g))
                    return true;
            return false;
        }

        private static bool EndWord(StringBuilder word)
        {
            bool hit = word.Length > 0 && Words.Contains(word.ToString());
            word.Clear();
            return hit;
        }

        // Türkçe I/İ/ı farkı kalksın
        private static string Normalize(string text) =>
            text.Replace('İ', 'i').Replace('I', 'i').Replace('ı', 'i').ToLowerInvariant();
    }
}
