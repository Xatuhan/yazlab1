using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YazLab.NpcDialogue
{
    public class GuardConversation
    {
        private readonly ILlmClient _client;
        private readonly IResponseParser _parser;
        private readonly ConversationHistory _history;
        private readonly int _maxInputChars;
        private readonly int _patienceTurns;
        private readonly string _patienceText;
        private readonly HashSet<string> _shownItems = new HashSet<string>();
        private readonly string[] _doubtNotes;
        private readonly string _proofsNote;

        // doubtNotes[i]: (i+1). kanıttan sonra şefe verilen durum notu; kapı (notlar + 1). kanıtta açılır.
        // proofsNote: satır başına bir aşama; "{n}" kanıt sayısıdır
        public GuardConversation(ILlmClient client, IResponseParser parser, ConversationHistory history, int maxInputChars,
                                 int patienceTurns = int.MaxValue, string patienceText = null,
                                 string[] doubtNotes = null, string proofsNote = null)
        {
            _client = client;
            _parser = parser;
            _history = history;
            _maxInputChars = maxInputChars;
            _patienceTurns = patienceTurns;
            _patienceText = patienceText;
            _doubtNotes = doubtNotes ?? Array.Empty<string>();
            _proofsNote = proofsNote;
        }

        public int Proofs { get; private set; }

        public bool IsBusy { get; private set; }

        public int AnsweredTurns { get; private set; }

        public bool WasShown(string itemId) => _shownItems.Contains(itemId);

        public Task<TurnOutcome> SendAsync(string playerText, CancellationToken ct)
        {
            if (IsBusy)
                return Task.FromResult(TurnOutcome.Busy());

            string text = Clean(playerText);
            if (text.Length == 0 || text.Length > _maxInputChars || InputFilter.IsBlocked(text))
                return Task.FromResult(TurnOutcome.Rejected());

            return RunTurnAsync(text, null, ct);
        }

        public Task<TurnOutcome> ShowItemAsync(ItemData item, CancellationToken ct)
        {
            if (IsBusy)
                return Task.FromResult(TurnOutcome.Busy());
            if (item == null || _shownItems.Contains(item.id)
                || item.eventText == null || !item.eventText.StartsWith(ItemCatalog.EventPrefix, StringComparison.Ordinal))
                return Task.FromResult(TurnOutcome.Rejected());

            return RunTurnAsync(item.eventText, item.id, ct);
        }

        private async Task<TurnOutcome> RunTurnAsync(string content, string itemId, CancellationToken ct)
        {
            bool lastChance = AnsweredTurns + 1 >= _patienceTurns;

            IsBusy = true;
            try
            {
                (NpcDecision decision, LlmError error) = await AskAsync(content, Join(ProofsNote(), lastChance ? _patienceText : null), ct);
                if (error != LlmError.None)
                    return TurnOutcome.Failed(error);

                // şef kolay inanmaz: son kanıta kadar "allow" bir kanıt sayılır, cevabı durum notuyla yeniden kurulur
                bool proofThisTurn = decision.Action == NpcAction.Allow && Proofs < _doubtNotes.Length;
                if (proofThisTurn)
                {
                    // cevap "kapıyı açıyorum" demesin diye bir kez daha sorulabilir
                    for (int attempt = 0; attempt < 2 && decision.Action == NpcAction.Allow; attempt++)
                    {
                        (decision, error) = await AskAsync(content, _doubtNotes[Proofs], ct);
                        if (error != LlmError.None)
                            return TurnOutcome.Failed(error);
                    }
                    Proofs++;
                    if (decision.Action == NpcAction.Allow)
                        decision = new NpcDecision(NpcAction.Deny, decision.Reply);
                }

                // sabır bitti, oyun sonsuza kadar sürmesin
                if (lastChance && !proofThisTurn && decision.Action == NpcAction.Deny)
                    decision = new NpcDecision(NpcAction.Attack, decision.Reply);

                _history.AddTurn(content, decision);
                AnsweredTurns++;
                if (itemId != null)
                    _shownItems.Add(itemId);
                return TurnOutcome.Answered(decision);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<(NpcDecision, LlmError)> AskAsync(string content, string note, CancellationToken ct)
        {
            LlmResult result = await _client.SendAsync(_history.BuildMessages(content, note), ct);
            if (!result.Ok)
                return (default, result.Error);
            if (!_parser.TryParse(result.Content, out NpcDecision decision))
                return (default, LlmError.BadResponse);
            return (decision, LlmError.None);
        }

        // satır n: n kanıttan sonraki not; oyuncu metni sistem kanalına girmez, yalnızca sayı
        private string ProofsNote()
        {
            if (Proofs == 0 || _proofsNote == null)
                return null;
            string[] lines = _proofsNote.Split('\n');
            return lines[Math.Min(Proofs, lines.Length) - 1].Trim().Replace("{n}", Proofs.ToString());
        }

        private static string Join(string a, string b) =>
            a == null ? b : b == null ? a : a + "\n" + b;

        // satır sonuyla sahte blok kurulamasın
        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
                sb.Append(char.IsControl(c) ? ' ' : c);
            return sb.ToString().Trim();
        }
    }
}
