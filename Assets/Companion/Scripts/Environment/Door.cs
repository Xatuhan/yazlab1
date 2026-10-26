using System;
using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour, IActivatable
{
    public bool IsOpen { get; private set; } = false;

    // IActivatable aray�z�n�n �art�:
    public bool IsActivated => IsOpen;

    [Header("Bile�enler & Ba�lant�lar")]
    [SerializeField] private NavMeshObstacle navMeshObstacle;
    [SerializeField] private Animator doorAnimator;

    public static event Action<Door> OnDoorOpened;

    private void Start()
    {
        Debug.Log($"<color=yellow>{gameObject.name} �zerindeki Door script'i aktif ve �al���yor!</color>");
    }

    private void Awake()
    {
        if (navMeshObstacle == null) navMeshObstacle = GetComponent<NavMeshObstacle>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
            Debug.Log("[Door] Look at the door and press E to send the robot.");
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
            Debug.Log("<color=cyan>[Oyuncu]: Robota kap� �evresini ara�t�rmas� emredildi!</color>");
            robot.StateMachine.ChangeState(new AI.States.PetInvestigateState(robot, this));
        }
        else
        {
            Debug.LogWarning("[Kap�]: Sahnede RobotBrain bulunamad�!");
        }
    }

    /// <summary>
    /// IActivatable aray�z� tetikleme metodu. 
    /// Genel terminal/�alter mekanizmas� buray� �a��r�r.
    /// </summary>
    public void Activate()
    {
        UnlockAndOpen();
    }

    public void UnlockAndOpen()
    {
        if (IsOpen) return;

        IsOpen = true;
        Debug.Log($"<color=green>[Kap�]: A��l�yor!</color>");

        // 1. NavMesh �zerindeki yapay zeka engelini kald�r
        if (navMeshObstacle != null)
        {
            navMeshObstacle.enabled = false;
        }

        // 2. Prefab hiyerar�isindeki T�M kat� (Is Trigger = false) fizik collider'lar�n� kapat
        // transform.root sayesinde script ister panelde ister ana objede olsun t�m kat� engelleri temizler
        Collider[] allColliders = transform.root.GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            if (!col.isTrigger)
            {
                col.enabled = false;
            }
        }

        // 3. G�rsel a��lma efekti / Animasyon
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

