using System;
using UnityEngine;

namespace YazLab.NpcDialogue
{
    [Serializable]
    public class OllamaSettings
    {
        public string baseUrl;
        public string model;
        public int timeoutSeconds;
        public int numCtx;
        public int numPredict;
        public float temperature;
        public string keepAlive;
        public int maxInputChars;
        public int maxReplyChars;
        public int historyTurns;
        public int patienceTurns;

        public static bool TryParse(string json, out OllamaSettings settings, out string error)
        {
            settings = null;
            OllamaSettings s;
            try
            {
                s = JsonUtility.FromJson<OllamaSettings>(json);
            }
            catch (Exception)
            {
                error = "Ayar dosyası okunamadı (bozuk JSON).";
                return false;
            }

            if (s == null)
            {
                error = "Ayar dosyası boş.";
                return false;
            }
            if (!IsLocalHttp(s.baseUrl))
            {
                error = "Ollama adresi yalnızca http://127.0.0.1 olabilir.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(s.model) || string.IsNullOrWhiteSpace(s.keepAlive))
            {
                error = "Ayar dosyasında model adı ya da keepAlive eksik.";
                return false;
            }
            if (s.timeoutSeconds <= 0 || s.numCtx <= 0 || s.numPredict <= 0 || s.maxInputChars <= 0
                || s.maxReplyChars <= 0 || s.historyTurns <= 0 || s.patienceTurns <= 0)
            {
                error = "Ayar dosyasındaki sayılar sıfırdan büyük olmalı.";
                return false;
            }
            if (s.temperature <= 0f || s.temperature > 2f)
            {
                error = "Ayar dosyasındaki temperature 0 ile 2 arasında olmalı.";
                return false;
            }

            settings = s;
            error = null;
            return true;
        }

        // oyuncu yazısı bu bilgisayardan çıkmasın
        private static bool IsLocalHttp(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
                && uri.Scheme == Uri.UriSchemeHttp
                && uri.Host == "127.0.0.1"
                && string.IsNullOrEmpty(uri.UserInfo);
        }
    }
}
