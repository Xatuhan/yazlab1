using UnityEngine;

public class DoorTerminal : MonoBehaviour
{
    [Header("Kapý Baðlantýsý")]
    [Tooltip("Bu bilgisayarýn/terminalin açacaðý kapý")]
    [SerializeField] private Door targetDoor;

    [Header("Terminal Durumu")]
    [SerializeField] private bool isUsed = false;

    private void OnTriggerEnter(Collider other)
    {
        // Terminale henüz basýlmadýysa ve deðen kiþi Oyuncu veya Robot ise
        if (!isUsed && (other.CompareTag("Player") || other.name.Contains("Robot") || other.GetComponent<RobotBrain>() != null))
        {
            ActivateTerminal();
        }
    }

    public void ActivateTerminal()
    {
        if (isUsed) return;

        isUsed = true;
        Debug.Log($"<color=cyan>[Terminal]: Bilgisayar butonuna basýldý! Kapý tetikleniyor...</color>");

        // Baðlý olan kapýyý aç
        if (targetDoor != null)
        {
            targetDoor.UnlockAndOpen();
        }
        else
        {
            Debug.LogWarning("[Terminal]: Hedef kapý (Target Door) atanmamýþ!");
        }

        // Ýsteðe baðlý: Terminal ekranýnýn rengini yeþile çevirme / ses çalma
    }
}