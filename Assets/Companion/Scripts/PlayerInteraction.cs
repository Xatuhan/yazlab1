using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Etkileþim Ayarlarý")]
    public float interactDistance = 5.0f; // Kamera arkada olduðu için mesafeyi 5m yaptýk
    public LayerMask ignoreLayers;        // Karakterin kendisine çarpmamasý için Player Layer'ý seçeceðiz

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Ekranýn tam ortasýndan (bakýþ yönünden) ýþýn fýrlat
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green, 2.0f);

        // Belirtilen layer'lar hariç raycast fýrlat
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, ~ignoreLayers, QueryTriggerInteraction.Collide))
        {
            Debug.Log("Iþýnýn çarptýðý obje: " + hit.collider.name);

            AirVents vent = hit.collider.GetComponentInParent<AirVents>();
            if (vent != null)
            {
                vent.OpenVent();
            }
        }
    }
}