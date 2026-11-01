namespace YazLab.NpcDialogue
{
    public interface IResponseParser
    {
        bool TryParse(string content, out NpcDecision decision);
    }
}
