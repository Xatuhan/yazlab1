using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Etkileşim Ayarları")]
    public float interactDistance = 5.0f; // Kamera arkada olduğu için mesafeyi 5m yaptık
    public LayerMask ignoreLayers;        // Karakterin kendisine çarpmaması için Player Layer'ı seçeceğiz

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

        // Ekranın tam ortasından (bakış yönünden) ışın fırlat
        if (mainCamera == null) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green, 2.0f);

        // Belirtilen layer'lar hariç raycast fırlat
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, ~ignoreLayers, QueryTriggerInteraction.Collide))
        {
           

            Door door = hit.collider.GetComponentInParent<Door>();
            if (door == null)
            {
                DoorTerminal terminal = hit.collider.GetComponentInParent<DoorTerminal>();
                if (terminal != null) door = terminal.TargetDoor;
            }

            if (door != null)
            {
                door.CommandRobotToInvestigate();
                return;
            }

            
        }
    }
}