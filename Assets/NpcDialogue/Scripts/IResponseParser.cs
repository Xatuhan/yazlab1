namespace YazLab.NpcDialogue
{
    // Model metnini karara çevirir. Ayrı arayüz: cevap biçimi değişirse yalnızca ayrıştırıcı değişir.
    public interface IResponseParser
    {
        // Metin anlaşılamazsa false döner; çağıran bunu sistem mesajına çevirir.
        bool TryParse(string content, out NpcDecision decision);
    }
}
