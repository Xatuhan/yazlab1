namespace YazLab.NpcDialogue
{
    public enum LlmError
    {
        None,
        ServerUnreachable,
        ModelNotFound,
        Timeout,
        BadResponse
    }

    public class LlmResult
    {
        public LlmError Error { get; }
        public string Content { get; }
        public bool Ok => Error == LlmError.None;

        private LlmResult(LlmError error, string content)
        {
            Error = error;
            Content = content;
        }

        public static LlmResult Success(string content) => new LlmResult(LlmError.None, content);
        public static LlmResult Fail(LlmError error) => new LlmResult(error, null);
    }
}
