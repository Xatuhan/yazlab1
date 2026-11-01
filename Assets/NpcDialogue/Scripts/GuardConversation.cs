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

        public GuardConversation(ILlmClient client, IResponseParser parser, ConversationHistory history, int maxInputChars,
                                 int patienceTurns = int.MaxValue, string patienceText = null)
        {
            _client = client;
            _parser = parser;
            _history = history;
            _maxInputChars = maxInputChars;
            _patienceTurns = patienceTurns;
            _patienceText = patienceText;
        }

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
                LlmResult result = await _client.SendAsync(
                    _history.BuildMessages(content, lastChance ? _patienceText : null), ct);
                if (!result.Ok)
                    return TurnOutcome.Failed(result.Error);
                if (!_parser.TryParse(result.Content, out NpcDecision decision))
                    return TurnOutcome.Failed(LlmError.BadResponse);

                // sabır bitti, oyun sonsuza kadar sürmesin
                if (lastChance && decision.Action == NpcAction.Deny)
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
