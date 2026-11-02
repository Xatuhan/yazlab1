
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
            Debug.Log("Oyuncu lazere temas etti! Baþlangýca dönülüyor...");
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

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        if (cc != null)
            cc.enabled = true;
    }
}
