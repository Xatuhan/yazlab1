namespace YazLab.NpcDialogue
{
    // Model cevabından ayrıştırılmış sonuç: oyun yalnızca bu iki değeri kullanır.
    public readonly struct NpcDecision
    {
        public NpcAction Action { get; }
        public string Reply { get; }

        public NpcDecision(NpcAction action, string reply)
        {
            Action = action;
            Reply = reply;
        }
    }
}
