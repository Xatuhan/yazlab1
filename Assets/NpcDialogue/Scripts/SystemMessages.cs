namespace YazLab.NpcDialogue
{
    // Oyuncuya gösterilen sistem mesajları (bekçinin sözü değil; ayrı sarı satırda çıkar).
    // Hepsi tek yerde: metin değişirse tek dosya değişir. PDF 2.9 / 4.3: anlamlı geri bildirim.
    public static class SystemMessages
    {
        public const string InputRejected = "Bu mesaj gönderilmedi: oyun komutu ya da izin verilmeyen işaret içeriyor.";
        public const string FilesUnreadable = "Bekçinin ayar ya da metin dosyası okunamadı.";
        public const string Unexpected = "Beklenmeyen bir hata oldu. Tekrar deneyin.";

        public static string For(LlmError error, string model, int timeoutSeconds)
        {
            switch (error)
            {
                case LlmError.ServerUnreachable:
                    return "Ollama'ya ulaşılamıyor. Ollama uygulamasını açıp tekrar deneyin.";
                case LlmError.ModelNotFound:
                    return $"'{model}' modeli bulunamadı. Terminalde 'ollama pull {model}' ile indirin.";
                case LlmError.Timeout:
                    return $"Model {timeoutSeconds} saniyede cevap vermedi. Tekrar deneyin.";
                case LlmError.BadResponse:
                    return "Bekçinin cevabı anlaşılamadı. Tekrar deneyin.";
                default:
                    return Unexpected;
            }
        }
    }
}
