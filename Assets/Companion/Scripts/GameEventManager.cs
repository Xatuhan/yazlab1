using System;

public static class GameEventManager
{
    public static event Action OnDoorUnlocked;
    public static event Action OnPlayerDefeated;

    public static void TriggerDoorUnlocked() => OnDoorUnlocked?.Invoke();
    public static void TriggerPlayerDefeated() => OnPlayerDefeated?.Invoke();
}