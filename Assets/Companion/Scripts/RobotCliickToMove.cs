using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotClickToMove : MonoBehaviour
{
    [Header("Ayar")]
    [SerializeField] private LayerMask groundLayer; // Sadece zemine týklanmasýný saðlamak için

    private NavMeshAgent agent;
    private Camera mainCamera;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Yeni Input System ile sol týk kontrolü
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            // Sadece zemin katmanýndaki týklamalarý algýla
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                agent.SetDestination(hit.point);
            }
        }
    }
}