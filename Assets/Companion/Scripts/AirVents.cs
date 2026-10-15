using UnityEngine;
using UnityEngine.AI;

public class AirVents : MonoBehaviour
{
    private NavMeshObstacle obstacle;
    private bool isOpen = false;

    void Start()
    {
        // Kapak üzerindeki NavMeshObstacle bileþenini al
        obstacle = GetComponent<NavMeshObstacle>();
    }

    // Oyuncu E tuþuna bastýðýnda tetiklenecek fonksiyon
    public void OpenVent()
    {
        if (isOpen) return; // Zaten açýksa tekrar çalýþtýrma
        isOpen = true;

        // 1. NavMesh engelini kaldýr (Robot için havalandýrma yolu açýlýr)
        if (obstacle != null)
        {
            obstacle.enabled = false;
        }

        // 2. Kapaðý hafif öne ve aþaðýya düþürerek yerinden sökülmüþ süsü ver
        transform.position += transform.forward * 0.2f + Vector3.down * 0.4f;
        transform.Rotate(0f, 0f, 80f);

        Debug.Log("Havalandýrma kapaðý açýldý! NavMesh yolu robot için temizlendi.");
    }
}