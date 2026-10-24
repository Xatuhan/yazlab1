using UnityEngine;

namespace YazLab.NpcDialogue
{
    // Kilitli kapı: yalnızca DoorUnlocked'ı dinler ve açılır (nesne kaybolur). Başka mantık yok.
    public class Door : MonoBehaviour
    {
        private void OnEnable() => NpcEvents.DoorUnlocked += Open;
        private void OnDisable() => NpcEvents.DoorUnlocked -= Open;

        private void Open(string reply, int turns) => gameObject.SetActive(false);
    }
}
