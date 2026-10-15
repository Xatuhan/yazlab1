using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Etkileþim Ayarlarý")]
    public float interactDistance = 3.0f; // E tuþuna basma mesafesi (metre)

    void Update()
    {
        // Oyuncu E tuþuna bastýðýnda
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        // Iþýný Kameradan DEÐÝL, Karakterin göðüs hizasýndan ileriye doðru fýrlat
        Vector3 rayOrigin = transform.position + Vector3.up * 1.2f; // Göðüs hizasý
        Ray ray = new Ray(rayOrigin, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            AirVents vent = hit.collider.GetComponent<AirVents>();
            if (vent != null)
            {
                vent.OpenVent();
            }
        }
    }
}