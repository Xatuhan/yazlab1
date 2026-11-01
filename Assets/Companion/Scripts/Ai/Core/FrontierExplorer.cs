using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Core
{
    public class FrontierExplorer : MonoBehaviour
    {
        [Header("Ağırlık Ayarları")]
        [SerializeField] private float infoGainWeight = 10.0f;
        [SerializeField] private float distanceWeight = 1.2f;
        [SerializeField] private float visitPenaltyWeight = 15.0f;

        [Header("Referanslar")]
        [SerializeField] private KnowledgeMap knowledgeMap;
        [SerializeField] private PathValidator pathValidator;

        private void Awake()
        {
            if (knowledgeMap == null) knowledgeMap = GetComponent<KnowledgeMap>();
            if (pathValidator == null) pathValidator = GetComponent<PathValidator>();
        }

        public ExplorationNode BestFrontierNode { get; private set; }


        public ExplorationNode EvaluateNextFrontier(Vector3 currentPosition)
        {
            if (knowledgeMap == null || knowledgeMap.AllNodes == null)
                return null;

            List<ExplorationNode> frontiers = GetFrontierNodes();

            Debug.Log($"[Frontier Debug] Bulunan frontier sayısı: {frontiers.Count}");

            if (frontiers.Count == 0)
            {
                Debug.LogWarning("[Frontier Debug] GetFrontierNodes() hiç aday döndürmedi.");
                BestFrontierNode = null;
                return null;
            }

            ExplorationNode bestNode = null;
            float highestScore = float.MinValue;
            NavMeshPath path = new NavMeshPath();

            foreach (var node in frontiers)
            {
                Debug.Log(
                    $"[Frontier Debug] Aday: {node.Position} | " +
                    $"Durum: {node.State} | Ziyaret: {node.VisitCount}"
                );

                if (pathValidator != null &&
                    !pathValidator.IsPathFullyExplored(currentPosition, node.Position))
                {
                    Debug.LogWarning(
                        $"[Frontier Debug] PathValidator eledi: {node.Position}"
                    );
                    continue;
                }

                // Bilgi kazancı: Çevresindeki Unknown sayısı
                int unknownNeighbors = 0;

                foreach (var n in node.Neighbors)
                {
                    if (n.State == NodeState.Unknown)
                        unknownNeighbors++;
                }

                // NavMesh gerçek yürüme mesafesi
                float pathDistance = float.MaxValue;

                bool pathCalculated = NavMesh.CalculatePath(
                    currentPosition,
                    node.Position,
                    NavMesh.AllAreas,
                    path
                );

                if (pathCalculated && path.status == NavMeshPathStatus.PathComplete)
                {
                    pathDistance = GetPathLength(path);
                }

                if (pathDistance == float.MaxValue)
                {
                    Debug.LogWarning(
                        $"[Frontier Debug] NavMesh rotası tamamlanamadı: {node.Position} | " +
                        $"CalculatePath: {pathCalculated} | " +
                        $"PathStatus: {path.status}"
                    );
                    continue;
                }

                // Skor denklemi — mevcut mantık korundu
                float score = (unknownNeighbors * infoGainWeight)
                            - (pathDistance * distanceWeight)
                            - (node.VisitCount * visitPenaltyWeight);

                Debug.Log(
                    $"[Frontier Debug] Aday geçerli: {node.Position} | " +
                    $"Bilinmeyen komşu: {unknownNeighbors} | " +
                    $"Rota mesafesi: {pathDistance:F2} | " +
                    $"Skor: {score:F2}"
                );

                if (score > highestScore)
                {
                    highestScore = score;
                    bestNode = node;
                }
            }

            if (bestNode != null)
            {
                Debug.Log(
                    $"[FrontierExplorer] Seçilen hedef: {bestNode.Position} | " +
                    $"Durum: {bestNode.State} | " +
                    $"Ziyaret: {bestNode.VisitCount} | " +
                    $"Komşu sayısı: {bestNode.Neighbors.Count} | " +
                    $"Skor: {highestScore:F2}"
                );
            }
            else
            {
                Debug.LogWarning(
                    "[FrontierExplorer] Uygun keşif hedefi bulunamadı. " +
                    "Aday, PathValidator veya NavMesh kontrollerinde elenmiş olabilir."
                );
            }

            BestFrontierNode = bestNode;
            return bestNode;
        }


        private List<ExplorationNode> GetFrontierNodes()
        {
            List<ExplorationNode> list = new List<ExplorationNode>();

            int seenCount = 0;
            int visitedCount = 0;
            int unknownCount = 0;
            int checkedCount = 0;
            int knownWithUnknownNeighbor = 0;

            foreach (var node in knowledgeMap.AllNodes)
            {
                if (node.State == NodeState.Unknown)
                    unknownCount++;
                else if (node.State == NodeState.Checked)
                    checkedCount++;
                else if (node.State == NodeState.Seen)
                    seenCount++;
                else if (node.State == NodeState.Visited)
                    visitedCount++;

                if (node.State == NodeState.Checked)
                    continue;

                // Frontier şartı: Seen veya Visited + en az 1 Unknown komşu
                if (node.State == NodeState.Seen ||
                    node.State == NodeState.Visited)
                {
                    foreach (var n in node.Neighbors)
                    {
                        if (n.State == NodeState.Unknown)
                        {
                            list.Add(node);
                            knownWithUnknownNeighbor++;
                            break;
                        }
                    }
                }
            }

            Debug.Log(
                $"[Frontier Debug] Toplam düğüm: {knowledgeMap.AllNodes.Count} | " +
                $"Unknown: {unknownCount} | Seen: {seenCount} | " +
                $"Visited: {visitedCount} | Checked: {checkedCount} | " +
                $"Frontier: {list.Count} | " +
                $"Unknown komşuluğu olan bilinen düğüm: {knownWithUnknownNeighbor}"
            );
            int checkedWithUnknownNeighbor = 0;
            int seenVisitedWithUnknownNeighbor = 0;

            foreach (var node in knowledgeMap.AllNodes)
            {
                bool hasUnknownNeighbor = false;

                foreach (var neighbor in node.Neighbors)
                {
                    if (neighbor.State == NodeState.Unknown)
                    {
                        hasUnknownNeighbor = true;
                        break;
                    }
                }

                if (!hasUnknownNeighbor)
                    continue;

                if (node.State == NodeState.Checked)
                {
                    checkedWithUnknownNeighbor++;

                    Debug.Log(
                        $"[Frontier Diagnosis] Checked sınır düğümü: " +
                        $"{node.Position} | Komşu sayısı: {node.Neighbors.Count}"
                    );
                }
                else if (node.State == NodeState.Seen ||
                         node.State == NodeState.Visited)
                {
                    seenVisitedWithUnknownNeighbor++;
                }
            }

            Debug.Log(
                $"[Frontier Diagnosis] Unknown komşusu olan Checked: " +
                $"{checkedWithUnknownNeighbor} | " +
                $"Unknown komşusu olan Seen/Visited: " +
                $"{seenVisitedWithUnknownNeighbor}"
            );
            return list;
        }


        private float GetPathLength(NavMeshPath path)
        {
            float len = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                len += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return len;
        }
    }
}