using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Core
{
    public enum NodeState { Unknown, Seen, Visited, Checked }

    public class ExplorationNode
    {
        public Vector3 Position { get; private set; }
        public NodeState State { get; set; }
        public int VisitCount { get; set; }
        public List<ExplorationNode> Neighbors { get; private set; }

        public ExplorationNode(Vector3 position)
        {
            Position = position;
            State = NodeState.Unknown;
            VisitCount = 0;
            Neighbors = new List<ExplorationNode>();
        }
    }

    public class KnowledgeMap : MonoBehaviour
    {
        [Header("Grid & Düğüm Ayarları")]
        [SerializeField] private float nodeSpacing = 2.0f; // Düğümler arası mesafe (metre)
        [SerializeField] private float neighborDistance = 3.5f;

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;

        public List<ExplorationNode> AllNodes { get; private set; } = new List<ExplorationNode>();
        public float NodeSpacing => nodeSpacing;

        private void Awake()
        {
            GenerateGraphFromNavMesh();
        }

        /// <summary>
        /// NavMesh üçgenleme verisini kullanarak sahnede yürünebilir düğüm ağı oluşturur.
        /// </summary>
        /// <summary>
        /// NavMesh'in kapladığı tüm 3D alana düzenli ızgara (Grid) atarak
        /// kapı eşikleri, tüneller ve odalar dahil eksiksiz düğüm yerleştirir.
        /// </summary>
        public void GenerateGraphFromNavMesh()
        {
            AllNodes.Clear();
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices.Length == 0) return;

            // 1. Sahnede pişirilmiş NavMesh'in dış sınırlarını (Bounds) hesapla
            Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            foreach (var v in triangulation.vertices)
            {
                bounds.Encapsulate(v);
            }

            List<Vector3> sampledPositions = new List<Vector3>();
            float step = nodeSpacing; // Inspector'daki Node Spacing (örn: 1.2m)

            // 2. Tüm haritayı X, Y, Z eksenlerinde düzenli adımlarla tara
            for (float x = bounds.min.x - 1f; x <= bounds.max.x + 1f; x += step)
            {
                for (float z = bounds.min.z - 1f; z <= bounds.max.z + 1f; z += step)
                {
                    for (float y = bounds.min.y - 1f; y <= bounds.max.y + 1f; y += step)
                    {
                        Vector3 testPos = new Vector3(x, y, z);

                        // Bu matematiksel ızgara noktasının altında/üstünde NavMesh var mı?
                        if (NavMesh.SamplePosition(testPos, out NavMeshHit hit, step * 0.6f, NavMesh.AllAreas))
                        {
                            // Çakışan/çok yakın olan noktaları ele
                            bool isTooClose = false;
                            foreach (var existing in sampledPositions)
                            {
                                if (Vector3.Distance(existing, hit.position) < step * 0.75f)
                                {
                                    isTooClose = true;
                                    break;
                                }
                            }

                            if (!isTooClose)
                            {
                                sampledPositions.Add(hit.position);
                            }
                        }
                    }
                }
            }

            // 3. Düğümleri oluştur
            foreach (var pos in sampledPositions)
            {
                AllNodes.Add(new ExplorationNode(pos));
            }

            // 4. Komşuluk bağlantılarını kur
            NavMeshPath path = new NavMeshPath();
            for (int i = 0; i < AllNodes.Count; i++)
            {
                for (int j = i + 1; j < AllNodes.Count; j++)
                {
                    float dist = Vector3.Distance(AllNodes[i].Position, AllNodes[j].Position);
                    if (dist <= neighborDistance)
                    {
                        if (NavMesh.CalculatePath(AllNodes[i].Position, AllNodes[j].Position, NavMesh.AllAreas, path))
                        {
                            if (path.status == NavMeshPathStatus.PathComplete)
                            {
                                AllNodes[i].Neighbors.Add(AllNodes[j]);
                                AllNodes[j].Neighbors.Add(AllNodes[i]);
                            }
                        }
                    }
                }
            }

            Debug.Log($"<color=cyan>[KnowledgeMap]: Izgara yöntemiyle {AllNodes.Count} adet düğüm eksiksiz oluşturuldu.</color>");
        }

        private void TryAddNodePosition(Vector3 pos, List<Vector3> list)
        {
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                foreach (var existing in list)
                {
                    if (Vector3.Distance(existing, hit.position) < nodeSpacing)
                        return;
                }
                list.Add(hit.position);
            }
        }

        /// <summary>
        /// Şalter bulunduğunda veya labirent geçildiğinde Checked cezalarını temizler.
        /// </summary>
        public void ResetCheckedNodes()
        {
            foreach (var node in AllNodes)
            {
                if (node.State == NodeState.Checked)
                {
                    node.State = NodeState.Seen;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || AllNodes == null) return;

            foreach (var node in AllNodes)
            {
                switch (node.State)
                {
                    case NodeState.Unknown:
                        Gizmos.color = new Color(1f, 0f, 0f, 0.4f); // Kırmızı
                        break;
                    case NodeState.Seen:
                        Gizmos.color = new Color(0f, 1f, 0f, 0.6f); // Yeşil
                        break;
                    case NodeState.Visited:
                        Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f); // Mavi
                        break;
                    case NodeState.Checked:
                        Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.3f); // Gri
                        break;
                }
                Gizmos.DrawSphere(node.Position + Vector3.up * 0.2f, 0.25f);
            }
        }
    }
}