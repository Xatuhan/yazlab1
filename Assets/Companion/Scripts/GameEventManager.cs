using System;
using UnityEngine;

public static class GameEventManager
{
    // 3 Temel Olay (Event)
    public static event Action OnObstacleCleared;
    public static event Action OnDoorUnlocked;
    public static event Action OnPlayerDefeated;

    // Tetikleme Fonksiyonlarý (Triggers)
    public static void TriggerObstacleCleared() => OnObstacleCleared?.Invoke();
    public static void TriggerDoorUnlocked() => OnDoorUnlocked?.Invoke();
    public static void TriggerPlayerDefeated() => OnPlayerDefeated?.Invoke();
}