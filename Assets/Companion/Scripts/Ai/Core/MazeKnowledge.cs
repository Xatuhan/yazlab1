
using System.Collections.Generic;
using UnityEngine;

public class MazeKnowledge : MonoBehaviour
{
    public enum CellState
    {
        Unknown,
        Open,
        Wall
    }

    [System.Serializable]
    public class CellData
    {
        public CellState state = CellState.Unknown;
        public int visitCount;
        public float lastObservedTime;
    }

    [Header("Harita Ayarlarý")]
    [SerializeField] private float cellSize = 0.5f;

    [Header("Keþif Sýnýrý")]
    [SerializeField] private BoxCollider mazeBounds;

    [Header("Görselleþtirme")]
    [SerializeField] private bool showMapGizmos = true;
    [SerializeField] private float gizmoHeight = 0.05f;

    private readonly Dictionary<Vector2Int, CellData> cells =
        new Dictionary<Vector2Int, CellData>();

    public int DiscoveredCellCount => cells.Count;

    // Pozisyonun Maze sýnýrlarý içinde olup olmadýðýný kontrol eder.
    public bool IsInsideMaze(Vector3 position)
    {
        if (mazeBounds == null)
            return false;

        Bounds bounds = mazeBounds.bounds;

        return position.x >= bounds.min.x &&
               position.x <= bounds.max.x &&
               position.z >= bounds.min.z &&
               position.z <= bounds.max.z;
    }

    // Önceki keþiflerden kalan haritayý temizler.
    public void ClearMap()
    {
        cells.Clear();

        
    }

    public Vector2Int WorldToCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            (cell.x + 0.5f) * cellSize,
            0f,
            (cell.y + 0.5f) * cellSize
        );
    }

    public CellState GetCellState(Vector2Int cell)
    {
        if (!cells.TryGetValue(cell, out CellData data))
            return CellState.Unknown;

        return data.state;
    }

    public int GetVisitCount(Vector2Int cell)
    {
        if (!cells.TryGetValue(cell, out CellData data))
            return 0;

        return data.visitCount;
    }

    public void RegisterVisit(Vector3 position)
    {
        if (!IsInsideMaze(position))
            return;

        Vector2Int cell = WorldToCell(position);
        CellData data = GetOrCreateCell(cell);

        data.visitCount++;
        data.state = CellState.Open;
        data.lastObservedTime = Time.time;
    }

    public void ObserveOpen(Vector3 position)
    {
        if (!IsInsideMaze(position))
            return;

        Vector2Int cell = WorldToCell(position);
        CellData data = GetOrCreateCell(cell);

        if (data.state != CellState.Wall)
            data.state = CellState.Open;

        data.lastObservedTime = Time.time;
    }

    public void ObserveWall(Vector3 position)
    {
        if (!IsInsideMaze(position))
            return;

        Vector2Int cell = WorldToCell(position);
        CellData data = GetOrCreateCell(cell);

        data.state = CellState.Wall;
        data.lastObservedTime = Time.time;
    }

    public void ObserveClearRay(
        Vector3 origin,
        Vector3 direction,
        float distance,
        float sampleInterval = 0.25f)
    {
        if (distance <= 0f || sampleInterval <= 0f)
            return;

        Vector3 dir = direction.normalized;

        for (float d = 0f; d < distance; d += sampleInterval)
        {
            Vector3 sample = origin + dir * d;

            // Maze dýþýna çýkýldýðýnda taramayý durdur.
            if (!IsInsideMaze(sample))
                break;

            ObserveOpen(sample);
        }
    }

    private CellData GetOrCreateCell(Vector2Int cell)
    {
        if (!cells.TryGetValue(cell, out CellData data))
        {
            data = new CellData();
            cells.Add(cell, data);
        }

        return data;
    }

    private void OnDrawGizmos()
    {
        if (!showMapGizmos || cells == null)
            return;

        foreach (var pair in cells)
        {
            Vector3 center = CellToWorld(pair.Key);
            center.y = transform.position.y + gizmoHeight;

            switch (pair.Value.state)
            {
                case CellState.Open:
                    Gizmos.color = Color.green;
                    break;

                case CellState.Wall:
                    Gizmos.color = Color.red;
                    break;

                default:
                    continue;
            }

            Gizmos.DrawCube(
                center,
                new Vector3(
                    cellSize * 0.9f,
                    0.05f,
                    cellSize * 0.9f
                )
            );
        }
    }
}
