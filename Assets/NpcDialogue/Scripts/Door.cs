using UnityEngine;

namespace YazLab.NpcDialogue
{
    public class Door : MonoBehaviour
    {
        private void OnEnable() => NpcEvents.DoorUnlocked += Open;
        private void OnDisable() => NpcEvents.DoorUnlocked -= Open;

        private void Open(string reply, int turns) => gameObject.SetActive(false);
    }
}
