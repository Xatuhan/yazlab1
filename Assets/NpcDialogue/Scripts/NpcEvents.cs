using System;

namespace YazLab.NpcDialogue
{
    public static class NpcEvents
    {
        // dinleyiciler OnDisable'da ayrılmalı
        public static event Action<string, int> DoorUnlocked;
        public static event Action<string, int> PlayerDefeated;

        public static bool Publish(NpcDecision decision, int turns)
        {
            switch (decision.Action)
            {
                case NpcAction.Allow:
                    DoorUnlocked?.Invoke(decision.Reply, turns);
                    return true;
                case NpcAction.Attack:
                    PlayerDefeated?.Invoke(decision.Reply, turns);
                    return true;
                default:
                    return false;
            }
        }
    }
}
