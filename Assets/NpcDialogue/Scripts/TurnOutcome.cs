namespace YazLab.NpcDialogue
{
    public enum TurnStatus
    {
        Answered,
        Rejected,
        Busy,
        Failed
    }

    public readonly struct TurnOutcome
    {
        public TurnStatus Status { get; }
        public LlmError Error { get; }
        public NpcDecision Decision { get; }

        private TurnOutcome(TurnStatus status, LlmError error, NpcDecision decision)
        {
            Status = status;
            Error = error;
            Decision = decision;
        }

        public static TurnOutcome Answered(NpcDecision decision) => new TurnOutcome(TurnStatus.Answered, LlmError.None, decision);
        public static TurnOutcome Rejected() => new TurnOutcome(TurnStatus.Rejected, LlmError.None, default);
        public static TurnOutcome Busy() => new TurnOutcome(TurnStatus.Busy, LlmError.None, default);
        public static TurnOutcome Failed(LlmError error) => new TurnOutcome(TurnStatus.Failed, error, default);
    }
}
