using System;
using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour, IActivatable
{
    public bool IsOpen { get; private set; } = false;

    // IActivatable arayüzünün şartı:
    public bool IsActivated => IsOpen;

    [Header("Bileşenler & Bağlantılar")]
    [SerializeField] private NavMeshObstacle navMeshObstacle;
    [SerializeField] private Animator doorAnimator;

    public static event Action<Door> OnDoorOpened;

    private void Awake()
    {
        if (navMeshObstacle == null) navMeshObstacle = GetComponent<NavMeshObstacle>();
    }

    private static bool IsPlayerCollider(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerInteraction>() != null;
    }

    public void CommandRobotToInvestigate()
    {
        if (IsOpen) return;

        RobotBrain robot = FindAnyObjectByType<RobotBrain>();
        if (robot != null)
        {
            robot.StateMachine.ChangeState(new AI.States.PetInvestigateState(robot, this));
        }
        else
        {
            Debug.LogWarning("[Kapı]: Sahnede RobotBrain bulunamadı!");
        }
    }

    /// <summary>
    /// IActivatable arayüzü tetikleme metodu.
    /// Genel terminal/şalter mekanizması burayı çağırır.
    /// </summary>
    public void Activate()
    {
        UnlockAndOpen();
    }

    public void UnlockAndOpen()
    {
        if (IsOpen) return;

        IsOpen = true;

        // 1. NavMesh üzerindeki yapay zeka engelini kaldır
        if (navMeshObstacle != null)
        {
            navMeshObstacle.enabled = false;
        }

        // 2. Prefab hiyerarşisindeki TÜM katı (Is Trigger = false) fizik collider'larını kapat
        // transform.root sayesinde script ister panelde ister ana objede olsun tüm katı engelleri temizler
        Collider[] allColliders = transform.root.GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            if (!col.isTrigger)
            {
                col.enabled = false;
            }
        }

        // 3. Görsel açılma efekti / Animasyon
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