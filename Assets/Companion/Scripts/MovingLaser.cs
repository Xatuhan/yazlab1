using UnityEngine;

public class MovingLaser : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    [Tooltip("Lazerin baþlangýç noktasýndan ne kadar ve hangi yönde uzaklaþacaðý (Örn: Y ekseninde 2 birim yukari)")]
    [SerializeField] private Vector3 moveOffset = new Vector3(0f, 2f, 0f);

    [Tooltip("Hareket hýzý")]
    [SerializeField] private float speed = 2f;

    private Vector3 startPosition;

    private void Start()
    {
        // Baþlangýç pozisyonunu hafýzaya alýyoruz
        startPosition = transform.position;
    }

    private void Update()
    {
        // Time.time ile 0 ile 1 arasýnda sürekli gidip gelen bir pingPong deðeri üretiyoruz
        float pingPong = Mathf.PingPong(Time.time * speed, 1f);

        // Baþlangýç noktasý ile hedef offset noktasý arasýnda yumuþak geçiþ yapýyoruz
        transform.position = Vector3.Lerp(startPosition, startPosition + moveOffset, pingPong);
    }

    // Editörde yönü ve mesafeyi rahatça görebilmek için (Çizim Gizmo'su)
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + moveOffset);
        Gizmos.DrawWireSphere(origin + moveOffset, 0.1f);
    }
}