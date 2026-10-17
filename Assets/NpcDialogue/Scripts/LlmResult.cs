namespace YazLab.NpcDialogue
{
    // Oyuncuya ayrı sistem mesajı gösterilecek hata türleri.
    public enum LlmError
    {
        None,
        ServerUnreachable, // Ollama kapalı
        ModelNotFound,     // model indirilmemiş (404)
        Timeout,           // zamanında cevap yok
        BadResponse        // cevap boş ya da anlaşılamadı
    }

    // İstemcinin döndürdüğü ham sonuç: ya model metni ya da hata türü.
    public class LlmResult
    {
        public LlmError Error { get; }
        public string Content { get; }
        public bool Ok => Error == LlmError.None;

        private LlmResult(LlmError error, string content)
        {
            Error = error;
            Content = content;
        }

        public static LlmResult Success(string content) => new LlmResult(LlmError.None, content);
        public static LlmResult Fail(LlmError error) => new LlmResult(error, null);
    }
}
