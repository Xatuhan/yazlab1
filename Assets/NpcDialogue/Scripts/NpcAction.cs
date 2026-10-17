namespace YazLab.NpcDialogue
{
    // Bekçinin verebileceği üç karar. Deny = 0: tanımsız her durum varsayılan olarak reddedilir.
    public enum NpcAction
    {
        Deny = 0,
        Allow,
        Attack
    }
}
