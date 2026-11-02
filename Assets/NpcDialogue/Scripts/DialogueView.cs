using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace YazLab.NpcDialogue
{
    public class DialogueView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button sendButton;
        [FormerlySerializedAs("npcText")] // eski bağlantı korunsun
        [SerializeField] private TMP_Text logText;
        [SerializeField] private ScrollRect logScroll;
        [SerializeField] private TMP_Text systemText;
        [SerializeField] private TMP_Text thinkingText;
        [SerializeField] private Transform itemBar;
        [SerializeField] private Button itemButtonTemplate;
        [SerializeField] private GameObject talkHint;

        private const float DotInterval = 0.4f;
        private readonly Dictionary<string, Button> _itemButtons = new Dictionary<string, Button>();
        private readonly HashSet<string> _shownItems = new HashSet<string>();
        private const int MaxLogLines = 100;
        private readonly Queue<string> _log = new Queue<string>();
        private bool _busy;
        private bool _locked;
        private int _dots = -1;

        public event Action<string> Submitted;
        public event Action<string> ItemClicked;

        private void Awake()
        {
            input.richText = false;
            logText.richText = false; // etiketler düz metin görünsün
            systemText.richText = false;
            logText.text = string.Empty;
            input.lineType = TMP_InputField.LineType.SingleLine;

            sendButton.onClick.AddListener(Submit);
            input.onSubmit.AddListener(_ => Submit());
            thinkingText.gameObject.SetActive(false);
            if (itemButtonTemplate != null)
                itemButtonTemplate.gameObject.SetActive(false);
            panel.SetActive(false);
            ShowHint(false);
        }

        public void ShowHint(bool visible)
        {
            if (talkHint != null)
                talkHint.SetActive(visible);
        }

        public void SetItems(IReadOnlyList<ItemData> items)
        {
            if (itemBar == null || itemButtonTemplate == null || items == null)
                return;
            foreach (ItemData item in items)
            {
                Button button = Instantiate(itemButtonTemplate, itemBar);
                button.gameObject.SetActive(true);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = item.label;
                string id = item.id, name = item.label;
                button.onClick.AddListener(() => OnItemButton(id, name));
                _itemButtons[id] = button;
            }
        }

        public void MarkItemShown(string id)
        {
            _shownItems.Add(id);
            RefreshInteractable();
        }

        private void OnItemButton(string id, string label)
        {
            if (_busy || _locked || _shownItems.Contains(id))
                return;
            AddLine("(Gösterdin: " + label + ")");
            ItemClicked?.Invoke(id);
        }

        private void RefreshInteractable()
        {
            bool open = !_busy && !_locked;
            input.interactable = open;
            sendButton.interactable = open;
            foreach (KeyValuePair<string, Button> pair in _itemButtons)
                pair.Value.interactable = open && !_shownItems.Contains(pair.Key);
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
            AddLine("Güvenlik Şefi: " + reply);
            systemText.text = string.Empty;
        }

        private void AddLine(string line)
        {
            _log.Enqueue(line);
            while (_log.Count > MaxLogLines)
                _log.Dequeue();
            logText.text = string.Join("\n", _log);

            Canvas.ForceUpdateCanvases();
            logScroll.verticalNormalizedPosition = 0f;
        }

        public void ShowSystem(string message) => systemText.text = message;

        public void SetBusy(bool busy)
        {
            _busy = busy;
            RefreshInteractable();
            thinkingText.gameObject.SetActive(busy);
            _dots = -1;
            if (!busy && !_locked && panel.activeSelf)
                input.ActivateInputField();
        }

        public void Lock()
        {
            _locked = true;
            RefreshInteractable();
        }

        private void Update()
        {
            if (!_busy)
                return;
            int dots = (int)(Time.unscaledTime / DotInterval) % 4;
            if (dots == _dots)
                return;
            _dots = dots;
            thinkingText.text = "Düşünüyor" + new string('.', dots);
        }

        private void Submit()
        {
            if (_busy || _locked || string.IsNullOrWhiteSpace(input.text))
                return;
            string text = input.text;
            input.text = string.Empty;
            AddLine("Sen: " + text);
            Submitted?.Invoke(text);
        }
    }
}
