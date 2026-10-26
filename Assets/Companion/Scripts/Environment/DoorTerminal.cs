using System.Collections.Generic;
using UnityEngine;

public class DoorTerminal : MonoBehaviour
{
    [Header("Hedef Bağlantısı")]
    [Tooltip("Bu terminalin tetikleyeceği nesne (Door, LaserGroup vb.)")]
    [SerializeField] private MonoBehaviour targetToActivate;

    [Header("Terminal Durumu")]
    [SerializeField] private bool isUsed = false;

    public bool IsUsed => isUsed;
    public Door TargetDoor => targetToActivate as Door;

    public static readonly List<DoorTerminal> All = new List<DoorTerminal>();
    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public void ActivateTerminal()
    {
        if (isUsed) return;
        isUsed = true;
        Debug.Log("<color=cyan>[Terminal]: Bilgisayar butonuna basıldı! Hedef tetikleniyor...</color>");

        if (targetToActivate != null && targetToActivate is IActivatable activatable)
            activatable.Activate();
        else if (targetToActivate != null)
            Debug.LogWarning($"[Terminal]: {targetToActivate.name} nesnesi IActivatable arayüzünü içermiyor!");
        else
            Debug.LogWarning("[Terminal]: Hedef nesne (Target To Activate) atanmamış!");
    }
}
