using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace YazLab.NpcDialogue
{
    // Bekçinin kendisi: dosyaları okur, oyuncu yaklaşınca diyaloğu açar, turları GuardConversation'a verir.
    // Bekçiyi taşıyan nesnede trigger collider olmalı. Companion'ı tanımaz.
    public class GuardNpc : MonoBehaviour
    {
        [SerializeField] private DialogueView view;
        [SerializeField] private string playerTag = "Player";
        // Diyalogda kapatılır, sonra eski hâline döner (ör. A'nın PlayerMovement, PlayerInteraction, RobotClickToMove).
        // Böylece yazarken oyuncu yürümez, Gönder'e tıklamak robotu götürmez; A'nın koduna dokunulmaz.
        [SerializeField] private Behaviour[] disableWhileTalking = Array.Empty<Behaviour>();

        private const string Folder = "NpcDialogue";

        private OllamaClient _client;
        private GuardConversation _conversation;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private string _loadError; // null değilse NPC pasif, oyuncuya sistem mesajı gösterilir

        private bool _open;
        private bool[] _wasEnabled;
        private CursorLockMode _cursorLock;
        private bool _cursorVisible;

        private void Start()
        {
            view.Submitted += OnSubmitted;
            try
            {
                string dir = Path.Combine(Application.streamingAssetsPath, Folder);
                string json = File.ReadAllText(Path.Combine(dir, "ollama_settings.json"));
                if (!OllamaSettings.TryParse(json, out OllamaSettings settings, out _loadError))
                    return;

                string prompt = File.ReadAllText(Path.Combine(dir, "guard_prompt.txt"));
                string reminder = File.ReadAllText(Path.Combine(dir, "guard_reminder.txt"));
                _client = new OllamaClient(settings);
                _conversation = new GuardConversation(_client, new NpcResponseParser(settings.maxReplyChars),
                    new ConversationHistory(prompt, reminder, settings.historyTurns), settings.maxInputChars);
                view.SetMaxInput(settings.maxInputChars);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                _loadError = "Bekçinin ayar ya da metin dosyası okunamadı.";
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

            // Yazı kutusuna tıklanabilsin; kapanınca önceki duruma döner.
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
            if (!_open)
                return;
            _open = false;
            view.Hide();

            for (int i = 0; i < disableWhileTalking.Length; i++)
                if (disableWhileTalking[i] != null)
                    disableWhileTalking[i].enabled = _wasEnabled[i];
            Cursor.lockState = _cursorLock;
            Cursor.visible = _cursorVisible;
        }

        // async void yalnızca bu olay işleyicisinde; tüm gövde try/catch: hiçbir hata oyunu çökertmez.
        private async void OnSubmitted(string text)
        {
            if (_conversation == null || _conversation.IsBusy)
                return;

            view.SetBusy(true);
            try
            {
                TurnOutcome outcome = await _conversation.SendAsync(text, _cts.Token);
                Show(outcome);
            }
            catch (OperationCanceledException)
            {
                // Sahne kapandı; yapılacak bir şey yok.
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (view != null)
                    view.ShowSystem("Beklenmeyen bir hata oldu. Tekrar deneyin.");
            }
            finally
            {
                if (view != null) // nesne yok edildiyse arayüze dokunma
                    view.SetBusy(false);
            }
        }

        private void Show(TurnOutcome outcome)
        {
            switch (outcome.Status)
            {
                case TurnStatus.Answered:
                    view.ShowNpc(outcome.Decision.Reply); // allow/attack sonuçları Görev 5'te
                    break;
                case TurnStatus.Rejected:
                    view.ShowSystem("Bu mesaj gönderilmedi: oyun komutu ya da izin verilmeyen işaret içeriyor.");
                    break;
                case TurnStatus.Failed:
                    view.ShowSystem("Bekçiye ulaşılamadı: " + outcome.Error); // Türkçe metinler Görev 6'da
                    break;
            }
        }

        private void OnDestroy()
        {
            _cts.Cancel(); // süren istek iptal: cevap yok edilmiş nesneye dönmez
            _cts.Dispose();
            _client?.Dispose();
            if (view != null)
                view.Submitted -= OnSubmitted;
        }
    }
}
