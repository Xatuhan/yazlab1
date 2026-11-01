namespace YazLab.NpcDialogue
{
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
