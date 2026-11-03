
using System.Collections.Generic;
using UnityEngine;

public class LaserGroup : MonoBehaviour, IActivatable
{
    [Header("Lazer Işını Nesneleri (Manuel)")]
    [Tooltip("Kapatılacak lazer ışınları. Emitter modellerini eklemeyin.")]
    [SerializeField]
    private List<GameObject> laserBeams =
        new List<GameObject>();

    [Header("Otomatik Bulma Ayarları")]
    [Tooltip("Atanırsa yalnızca bu nesnenin altındaki eşleşen nesneler taranır.")]
    [SerializeField] private Transform laserParentGroup;

    [Tooltip("Otomatik bulmada aranacak nesne adı.")]
    [SerializeField] private string beamNameFilter = "Light";

    [Header("Oyuncuya Hasar Veren Lazer Collider'ları")]
    [Tooltip("Lazerler kapanınca devre dışı bırakılacak ayrı collider nesneleri.")]
    [SerializeField]
    private List<GameObject> laserHazardObjects =
        new List<GameObject>();

    [Header("Lazer Etkileşim Alanı")]
    [Tooltip("Lazerler kapanınca devre dışı bırakılacak Laser_Interaction_Zone.")]
    [SerializeField] private GameObject laserInteractionZone;

    public bool IsActivated { get; private set; } = false;

    private void Awake()
    {
        if (laserParentGroup != null)
        {
            Transform[] allChildren =
                laserParentGroup.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in allChildren)
            {
                if (child == laserParentGroup)
                    continue;

                if (string.IsNullOrEmpty(beamNameFilter) ||
                    child.name.ToLower().Contains(
                        beamNameFilter.ToLower()))
                {
                    if (!laserBeams.Contains(child.gameObject))
                    {
                        laserBeams.Add(child.gameObject);
                    }
                }
            }
        }
        else if (laserBeams.Count == 0)
        {
            Transform[] allSceneTransforms =
                FindObjectsByType<Transform>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            foreach (Transform t in allSceneTransforms)
            {
                if (!string.IsNullOrEmpty(beamNameFilter) &&
                    t.name.ToLower().Contains(
                        beamNameFilter.ToLower()))
                {
                    if (!laserBeams.Contains(t.gameObject))
                    {
                        laserBeams.Add(t.gameObject);
                    }
                }
            }

            
        }
    }

    public void Activate()
    {
        if (IsActivated)
            return;

        IsActivated = true;

        // 1. Lazer ışınlarını kapat.
        foreach (GameObject beam in laserBeams)
        {
            if (beam != null)
            {
                beam.SetActive(false);
            }
        }

        // 2. Oyuncuyu başlangıca döndüren ayrı collider nesnelerini kapat.
        foreach (GameObject hazardObject in laserHazardObjects)
        {
            if (hazardObject != null)
            {
                hazardObject.SetActive(false);
            }
        }

        // 3. Lazer etkileşim alanını kapat.
        if (laserInteractionZone != null)
        {
            laserInteractionZone.SetActive(false);
        }

    }
}
