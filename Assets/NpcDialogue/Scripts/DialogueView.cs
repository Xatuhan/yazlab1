using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace YazLab.NpcDialogue
{
    // Diyalog kutusu: yalnızca ekranla ilgilenir. Ne gönderileceğine ya da modele GuardNpc karar verir.
    public class DialogueView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button sendButton;
        [FormerlySerializedAs("npcText")] // eski alan adı: editördeki bağlantı korunur
        [SerializeField] private TMP_Text logText;      // sohbet geçmişi: "Sen:" ve "Bekçi:" satırları
        [SerializeField] private ScrollRect logScroll;  // geçmişi saran kaydırma alanı
        [SerializeField] private TMP_Text systemText;   // sistem mesajı: geçmişe girmez, ayrı satır
        [SerializeField] private TMP_Text thinkingText; // "Düşünüyor..."

        private const float DotInterval = 0.4f;
        // Ekrandaki geçmiş konuşmanın tamamını gösterir; sınır yalnızca belleğin sonsuz büyümemesi için.
        // (Modelin hafızası ayrı: ConversationHistory, son 6 tur.)
        private const int MaxLogLines = 100;
        private readonly Queue<string> _log = new Queue<string>();
        private bool _busy;
        private int _dots = -1;

        public event Action<string> Submitted;

        private void Awake()
        {
            // Model ya da oyuncu <color> gibi etiket yazsa da düz metin görünür.
            input.richText = false;
            logText.richText = false;
            systemText.richText = false;
            logText.text = string.Empty;
            input.lineType = TMP_InputField.LineType.SingleLine;

            sendButton.onClick.AddListener(Submit);
            input.onSubmit.AddListener(_ => Submit()); // Enter
            thinkingText.gameObject.SetActive(false);
            panel.SetActive(false);
        }

        private void OnDestroy()
        {
            sendButton.onClick.RemoveListener(Submit);
            input.onSubmit.RemoveAllListeners();
        }

        public void SetMaxInput(int maxChars) => input.characterLimit = maxChars;

        public void Show()
        {
            panel.SetActive(true);
            input.ActivateInputField();
        }

        public void Hide() => panel.SetActive(false);

        public void AddNpcLine(string reply)
        {
            AddLine("Bekçi: " + reply);
            systemText.text = string.Empty;
        }

        private void AddLine(string line)
        {
            _log.Enqueue(line);
            while (_log.Count > MaxLogLines)
                _log.Dequeue();
            logText.text = string.Join("\n", _log);

            // Yeni satır görünsün diye en alta kaydır (yerleşim önce güncellenir).
            Canvas.ForceUpdateCanvases();
            logScroll.verticalNormalizedPosition = 0f;
        }

        public void ShowSystem(string message) => systemText.text = message;

        // Bekleme sırasında giriş kapalı (çift gönderim yok), "Düşünüyor..." görünür; oyun akmaya devam eder.
        public void SetBusy(bool busy)
        {
            _busy = busy;
            input.interactable = !busy;
            sendButton.interactable = !busy;
            thinkingText.gameObject.SetActive(busy);
            _dots = -1;
            if (!busy && panel.activeSelf)
                input.ActivateInputField();
        }

        private void Update()
        {
            if (!_busy)
                return;
            // Noktalar 0,4 sn'de bir değişir: ekran donmadığını gösterir. timeScale'den bağımsız.
            // Metin yalnızca nokta sayısı değişince yazılır (her karede yeni string üretilmesin).
            int dots = (int)(Time.unscaledTime / DotInterval) % 4;
            if (dots == _dots)
                return;
            _dots = dots;
            thinkingText.text = "Düşünüyor" + new string('.', dots);
        }

        private void Submit()
        {
            if (_busy || string.IsNullOrWhiteSpace(input.text))
                return;
            string text = input.text;
            input.text = string.Empty;
            AddLine("Sen: " + text); // beklerken de ne yazıldığı görünsün
            Submitted?.Invoke(text);
        }
    }
}
