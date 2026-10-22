using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class Door : MonoBehaviour
{
    public bool IsOpen { get; private set; } = false;

    [Header("Bileþenler & Baðlantýlar")]
    [SerializeField] private NavMeshObstacle navMeshObstacle;
    [SerializeField] private Animator doorAnimator;

    private bool isPlayerInRange = false;

    public static event Action<Door> OnDoorOpened;

    private void Start()
    {
        Debug.Log($"<color=yellow>{gameObject.name} üzerindeki Door script'i aktif ve çalýþýyor!</color>");
    }

    private void Awake()
    {
        if (navMeshObstacle == null) navMeshObstacle = GetComponent<NavMeshObstacle>();
    }

    private void Update()
    {
        // Yeni Input System ile 'E' tuþu kontrolü
        bool isEPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (isPlayerInRange && !IsOpen && isEPressed)
        {
            TriggerRobotToInvestigate();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("<color=white>Kapýyý açmak için [E] tuþuna bas.</color>");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    private void TriggerRobotToInvestigate()
    {
        RobotBrain robot = FindAnyObjectByType<RobotBrain>();
        if (robot != null)
        {
            Debug.Log("<color=cyan>[Oyuncu]: Robota kapý çevresini araþtýrmasý emredildi!</color>");
            robot.StateMachine.ChangeState(new AI.States.PetInvestigateState(robot, this));
        }
        else
        {
            Debug.LogWarning("[Kapý]: Sahnede RobotBrain bulunamadý!");
        }
    }

    public void UnlockAndOpen()
    {
        if (IsOpen) return;

        IsOpen = true;
        Debug.Log($"<color=green>[Kapý]: Açýlýyor!</color>");

        // 1. NavMesh üzerindeki yapay zeka engelini kaldýr
        if (navMeshObstacle != null)
        {
            navMeshObstacle.enabled = false;
        }

        // 2. Prefab hiyerarþisindeki TÜM katý (Is Trigger = false) fizik collider'larýný kapat
        // transform.root sayesinde script ister panelde ister ana objede olsun tüm katý engelleri temizler
        Collider[] allColliders = transform.root.GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            if (!col.isTrigger)
            {
                col.enabled = false;
            }
        }

        // 3. Görsel açýlma efekti / Animasyon
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Open");
        }
        else
        {
            transform.position += Vector3.up * 3f;
        }

        OnDoorOpened?.Invoke(this);
    }
}