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
        private readonly int _patienceTurns;  // bu kadar turda ikna olmazsa saldırır (oyun sonsuza kadar sürmez)
        private readonly string _patienceText; // son turda modele giden "sabrın tükendi" kuralı

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

            // Son şans: model sabrının tükendiğini bilir ve kendi sözüyle saldırabilir.
            bool lastChance = AnsweredTurns + 1 >= _patienceTurns;

            IsBusy = true;
            try
            {
                LlmResult result = await _client.SendAsync(
                    _history.BuildMessages(text, lastChance ? _patienceText : null), ct);
                if (!result.Ok)
                    return TurnOutcome.Failed(result.Error);
                if (!_parser.TryParse(result.Content, out NpcDecision decision))
                    return TurnOutcome.Failed(LlmError.BadResponse); // bozuk tur geçmişe girmez

                // Güvence: son turda hâlâ "deny" ise oyun yine biter (PDF 1.1: oyun sona ermeli).
                if (lastChance && decision.Action == NpcAction.Deny)
                    decision = new NpcDecision(NpcAction.Attack, decision.Reply);

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
