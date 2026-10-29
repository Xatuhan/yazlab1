using UnityEngine;

public class PlayerLaserHandler : MonoBehaviour
{
    [Header("Yeniden Doðma Noktasý")]
    [SerializeField] private Transform spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.activeInHierarchy)
            return;

        if (other.name.Contains("Laser") || other.CompareTag("Laser"))
        {
            Debug.Log("<color=red>Oyuncu Lazere Temas Etti! Baþlangýca Dönülüyor...</color>");
            Respawn();
        }
    }

    private void Respawn()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn Point atanmadý! Lütfen Inspector'dan bir hedef Transform baðlayýn.");
            return;
        }

        // 1. CharacterController varsa geçici olarak kapatýyoruz (Pozisyon kilitlenmesini çözer)
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 2. Rigidbody varsa fizik hýzýný sýfýrlýyoruz
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3. Pozisyonu deðiþtiriyoruz
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        // 4. CharacterController'ý tekrar açýyoruz
        if (cc != null) cc.enabled = true;
    }
}