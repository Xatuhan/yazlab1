using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YazLab.NpcDialogue
{
    public class GuardNpc : MonoBehaviour
    {
        [SerializeField] private DialogueView view;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Behaviour[] disableWhileTalking = Array.Empty<Behaviour>();

        private const string Folder = "NpcDialogue";

        private OllamaClient _client;
        private OllamaSettings _settings;
        private GuardConversation _conversation;
        private ItemCatalog _items;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private string _loadError;

        private bool _open;
        private bool _finished;
        private bool[] _wasEnabled;
        private CursorLockMode _cursorLock;
        private bool _cursorVisible;

        private void Start()
        {
            view.Submitted += OnSubmitted;
            view.ItemClicked += ShowItem;
            try
            {
                string dir = Path.Combine(Application.streamingAssetsPath, Folder);
                string json = File.ReadAllText(Path.Combine(dir, "ollama_settings.json"));
                if (!OllamaSettings.TryParse(json, out OllamaSettings settings, out _loadError))
                    return;
                _settings = settings;

                string prompt = File.ReadAllText(Path.Combine(dir, "guard_prompt.txt"));
                string reminder = File.ReadAllText(Path.Combine(dir, "guard_reminder.txt"));
                string patience = File.ReadAllText(Path.Combine(dir, "guard_patience.txt"));
                _client = new OllamaClient(settings);
                _conversation = new GuardConversation(_client, new NpcResponseParser(settings.maxReplyChars),
                    new ConversationHistory(prompt, reminder, settings.historyTurns), settings.maxInputChars,
                    settings.patienceTurns, patience);
                view.SetMaxInput(settings.maxInputChars);
                LoadItems(Path.Combine(dir, "items.json"));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                _loadError = SystemMessages.FilesUnreadable;
            }
        }

        private void LoadItems(string path)
        {
            if (!File.Exists(path))
                return;
            if (ItemCatalog.TryParse(File.ReadAllText(path), out ItemCatalog catalog, out string error))
            {
                _items = catalog;
                view.SetItems(catalog.Items);
            }
            else
            {
                Debug.LogWarning(error);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
                Open();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
                Close();
        }

        private void Open()
        {
            if (_open)
                return;
            _open = true;

            _wasEnabled = new bool[disableWhileTalking.Length];
            for (int i = 0; i < disableWhileTalking.Length; i++)
            {
                if (disableWhileTalking[i] == null)
                    continue;
                _wasEnabled[i] = disableWhileTalking[i].enabled;
                disableWhileTalking[i].enabled = false;
            }

            _cursorLock = Cursor.lockState;
            _cursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            view.Show();
            if (_loadError != null)
                view.ShowSystem(_loadError);
        }

        private void Close()
        {
            if (!_open || _finished)
                return;
            _open = false;
            view.Hide();

            for (int i = 0; i < disableWhileTalking.Length; i++)
                if (disableWhileTalking[i] != null)
                    disableWhileTalking[i].enabled = _wasEnabled[i];
            Cursor.lockState = _cursorLock;
            Cursor.visible = _cursorVisible;
        }

        private void OnSubmitted(string text) => RunTurn(() => _conversation.SendAsync(text, _cts.Token), null);

        public void ShowItem(string itemId)
        {
            if (_items != null && _items.TryGet(itemId, out ItemData item))
                RunTurn(() => _conversation.ShowItemAsync(item, _cts.Token), itemId);
        }

        // tek async void; hata oyunu çökertmesin
        private async void RunTurn(Func<Task<TurnOutcome>> turn, string itemId)
        {
            if (_conversation == null || _conversation.IsBusy || _finished)
                return;

            view.SetBusy(true);
            try
            {
                TurnOutcome outcome = await turn();
                Show(outcome, itemId);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (view != null)
                    view.ShowSystem(SystemMessages.Unexpected);
            }
            finally
            {
                if (view != null)
                    view.SetBusy(false);
            }
        }

        private void Show(TurnOutcome outcome, string itemId)
        {
            switch (outcome.Status)
            {
                case TurnStatus.Answered:
                    if (itemId != null)
                        view.MarkItemShown(itemId);
                    view.AddNpcLine(outcome.Decision.Reply);
                    if (NpcEvents.Publish(outcome.Decision, _conversation.AnsweredTurns))
                    {
                        _finished = true;
                        view.Lock();
                    }
                    break;
                case TurnStatus.Rejected:
                    if (itemId == null)
                        view.ShowSystem(SystemMessages.InputRejected);
                    break;
                case TurnStatus.Failed:
                    view.ShowSystem(SystemMessages.For(outcome.Error, _settings.model, _settings.timeoutSeconds));
                    break;
            }
        }

        private void OnDestroy()
        {
            _cts.Cancel(); // cevap yok edilmiş nesneye dönmesin
            _cts.Dispose();
            _client?.Dispose();
            if (view != null)
            {
                view.Submitted -= OnSubmitted;
                view.ItemClicked -= ShowItem;
            }
        }
    }
}
