using System.Collections.Generic;
using UnityEngine;

public class LaserGroup : MonoBehaviour, IActivatable
{
    [Header("Lazer Iþýný Nesneleri (Manuel)")]
    [Tooltip("Sadece kapatýlacak lazer ýþýný/beam objelerini buraya ekleyin (Emitter modellerini eklemeyin).")]
    [SerializeField] private List<GameObject> laserBeams = new List<GameObject>();

    [Header("Otomatik Bulma Ayarlarý")]
    [Tooltip("Belirli bir ebeveynin altýndakileri taramak istersen buraya sürükle. Boþ býrakýrsan TÜM SAHNEDEKÝ eþleþen lazerleri otomatik bulur.")]
    [SerializeField] private Transform laserParentGroup;

    [Tooltip("Sadece adýnda bu kelime geçen objeler kapatýlýr (Örn: Light, Beam, Laser). Emitter modelleri korunur.")]
    [SerializeField] private string beamNameFilter = "Light";

    public bool IsActivated { get; private set; } = false;

    private void Awake()
    {
        // 1. Durum: Belirli bir Parent Obje atandýysa altýndaki objeleri filtreler
        if (laserParentGroup != null)
        {
            Transform[] allChildren = laserParentGroup.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in allChildren)
            {
                if (child == laserParentGroup) continue;

                if (string.IsNullOrEmpty(beamNameFilter) || child.name.ToLower().Contains(beamNameFilter.ToLower()))
                {
                    if (!laserBeams.Contains(child.gameObject))
                    {
                        laserBeams.Add(child.gameObject);
                    }
                }
            }
        }
        // 2. Durum: Parent ve Manuel Liste boþsa, sahnede adýnda beamNameFilter geçen TÜM ýþýnlarý otomatik toplar
        else if (laserBeams.Count == 0)
        {
            Transform[] allSceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Transform t in allSceneTransforms)
            {
                if (!string.IsNullOrEmpty(beamNameFilter) && t.name.ToLower().Contains(beamNameFilter.ToLower()))
                {
                    if (!laserBeams.Contains(t.gameObject))
                    {
                        laserBeams.Add(t.gameObject);
                    }
                }
            }

            Debug.Log($"<color=cyan>[LaserGroup]: Sahnedeki adýnda '{beamNameFilter}' geçen {laserBeams.Count} adet lazer ýþýný otomatik tespit edildi!</color>");
        }
    }

    public void Activate()
    {
        if (IsActivated) return;
        IsActivated = true;

        Debug.Log($"<color=green>[Lazer Grubu]: {gameObject.name} üzerindeki {laserBeams.Count} adet lazer ýþýný kapatýldý!</color>");

        // Sadece ýþýn/ýþýk nesnelerini kapatýr, Emitter 3D modelleri sahnede kalýr
        foreach (GameObject beam in laserBeams)
        {
            if (beam != null)
            {
                beam.SetActive(false);
            }
        }
    }
}