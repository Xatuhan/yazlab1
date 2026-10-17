using System;

namespace YazLab.NpcDialogue
{
    // Bekçi cevabının JSON sözleşmesi: {"action":"...","reply":"..."}. Hem ayrıştırıcı hem geçmiş kullanır.
    [Serializable]
    internal class ReplyJson
    {
        public string action;
        public string reply;
    }
}
