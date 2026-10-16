using UnityEngine;
using UnityEngine.AI;

public class AirVents : MonoBehaviour
{
    private NavMeshObstacle obstacle;
    private Collider ventCollider;
    private bool isOpen = false;

    void Start()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        ventCollider = GetComponent<Collider>();
    }

    public void OpenVent()
    {
        if (isOpen) return;
        isOpen = true;

        // 1. Kapaðýn Collider'ýný kapat (Karakteri zemin altýna itmesini engeller)
        if (ventCollider != null)
        {
            ventCollider.enabled = false;
        }

        // 2. NavMesh engelini kaldýr
        if (obstacle != null)
        {
            obstacle.enabled = false;
        }

        // 3. Kapaðý görsel olarak düþür
        transform.position += transform.forward * 0.2f + Vector3.down * 0.4f;
        transform.Rotate(0f, 0f, 80f);

        GameEventManager.TriggerObstacleCleared();

        Debug.Log("Havalandýrma kapaðý açýldý!");
    }
}