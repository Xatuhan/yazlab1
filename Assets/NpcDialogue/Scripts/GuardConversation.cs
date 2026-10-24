using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YazLab.NpcDialogue
{
    // Tek konuşma turunu yönetir: temizle → filtrele → geçmişle modele gönder → ayrıştır → geçmişe ekle.
    // MonoBehaviour değil: Unity'siz, sahte istemciyle test edilir.
    public class GuardConversation
    {
        private readonly ILlmClient _client;
        private readonly IResponseParser _parser;
        private readonly ConversationHistory _history;
        private readonly int _maxInputChars;

        public GuardConversation(ILlmClient client, IResponseParser parser, ConversationHistory history, int maxInputChars)
        {
            _client = client;
            _parser = parser;
            _history = history;
            _maxInputChars = maxInputChars;
        }

        // Aynı anda tek istek: model meşgulken ikinci mesaj gönderilmez.
        public bool IsBusy { get; private set; }

        // Bekçinin cevap verdiği toplam tur (bitiş ekranı için). Modelin hafızası ayrı: son 6 tur.
        public int AnsweredTurns { get; private set; }

        public async Task<TurnOutcome> SendAsync(string playerText, CancellationToken ct)
        {
            if (IsBusy)
                return TurnOutcome.Busy();

            string text = Clean(playerText);
            if (text.Length == 0 || text.Length > _maxInputChars || InputFilter.IsBlocked(text))
                return TurnOutcome.Rejected();

            IsBusy = true;
            try
            {
                LlmResult result = await _client.SendAsync(_history.BuildMessages(text), ct);
                if (!result.Ok)
                    return TurnOutcome.Failed(result.Error);
                if (!_parser.TryParse(result.Content, out NpcDecision decision))
                    return TurnOutcome.Failed(LlmError.BadResponse); // bozuk tur geçmişe girmez

                _history.AddTurn(text, decision);
                AnsweredTurns++;
                return TurnOutcome.Answered(decision);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Satır sonu gibi kontrol karakterleri boşluk olur: oyuncu yeni satırda sahte bir "sistem" bloğu kuramaz.
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
