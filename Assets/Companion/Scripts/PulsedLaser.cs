using System.Collections;
using UnityEngine;

public class PulsedLaser : MonoBehaviour
{
    [Header("Zamanlama Ayarlarý")]
    [Tooltip("Lazerin açýk kalacaðý süre (saniye)")]
    [SerializeField] private float onDuration = 2f;

    [Tooltip("Lazerin kapalý kalacaðý süre (saniye)")]
    [SerializeField] private float offDuration = 1.5f;

    [Tooltip("Baþlangýç gecikmesi")]
    [SerializeField] private float startDelay = 0f;

    [Header("Bileþen Referanslarý")]
    [Tooltip("Görsel bileþen (MeshRenderer veya Renderer)")]
    [SerializeField] private Renderer laserRenderer;

    [Tooltip("Tetikleyici collider bileþeni")]
    [SerializeField] private Collider laserCollider;

    private void Start()
    {
        // Bileþenler atanmadýysa otomatik çek
        if (laserCollider == null) laserCollider = GetComponent<Collider>();
        if (laserRenderer == null) laserRenderer = GetComponent<Renderer>();

        StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        if (startDelay > 0f)
        {
            SetLaserState(false);
            yield return new WaitForSeconds(startDelay);
        }

        while (true)
        {
            // Lazeri AÇ
            SetLaserState(true);
            yield return new WaitForSeconds(onDuration);

            // Lazeri KAPAT
            SetLaserState(false);
            yield return new WaitForSeconds(offDuration);
        }
    }

    private void SetLaserState(bool active)
    {
        // Objenin kendisini kapatmak yerine sadece bileþenleri açýp kapatýyoruz
        if (laserCollider != null)
            laserCollider.enabled = active;

        if (laserRenderer != null)
            laserRenderer.enabled = active;
    }
}