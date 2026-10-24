using System;

namespace YazLab.NpcDialogue
{
    // NPC'nin dışarıya duyurduğu oyun olayları. Kapı ve bitiş ekranı bunları dinler; companion'ı tanımaz.
    // Olay bekçinin son sözünü ve konuşmanın kaç turda bittiğini taşır (bitiş ekranında gösterilir).
    // Birleştirmede A'nın GameEventManager'ına köprüyle bağlanır; köprü bu iki bilgiyi yok sayabilir.
    // Dinleyiciler OnEnable'da abone olur, OnDisable'da ayrılır (static olayda sızıntı olmasın).
    public static class NpcEvents
    {
        public static event Action<string, int> DoorUnlocked;   // allow: kapı açılır (PDF: "kapının açılması")
        public static event Action<string, int> PlayerDefeated; // attack: savaş, oyuncu yenilir (PDF: "savaşın tetiklenmesi")

        // Kararı olaya çevirir. true: oyun bitti, diyalog kapanmalı. Deny: olay yok, kapı kilitli, konuşma sürer.
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
