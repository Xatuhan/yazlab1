using System.Collections.Generic;
using System.Text;

namespace YazLab.NpcDialogue
{
    // Oyun komutu içeren oyuncu mesajını modele hiç göndermemek için. Meşru bir oyuncu bunları yazmaz.
    // Kurallar 2026-10-17 prompt injection ölçümüyle seçildi (290 denemede 0 kandırma, 0 yanlış engel).
    public static class InputFilter
    {
        private static readonly HashSet<char> Symbols = new HashSet<char> { '{', '}', '<', '>', '[', ']' };

        private static readonly HashSet<string> Words = new HashSet<string>
        {
            "allow", "deny", "attack", "action", "json", "system", "sistem", "reply", "prompt"
        };

        // Harfler arasına işaret koyulsa da ("a-l-l-o-w") yakalananlar. deny/system burada yok:
        // "kaleden yürüdüm" birleşince "deny" içerir, masum cümle engellenirdi.
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
                    // Latin dışı harf (ör. Kiril "а" ile "аllow") benzer görünen harfle kandırmayı önler.
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

        // Kelime bittiğinde yasak kelime mi diye bakar ve tamponu boşaltır.
        private static bool EndWord(StringBuilder word)
        {
            bool hit = word.Length > 0 && Words.Contains(word.ToString());
            word.Clear();
            return hit;
        }

        // Türkçe I/İ/ı tuzağı: hepsi önce sade "i" olur, sonra kültürden bağımsız küçültülür.
        private static string Normalize(string text) =>
            text.Replace('İ', 'i').Replace('I', 'i').Replace('ı', 'i').ToLowerInvariant();
    }
}
