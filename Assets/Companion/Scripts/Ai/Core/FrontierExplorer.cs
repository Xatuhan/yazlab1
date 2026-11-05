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
            if (knowledgeMap == null)
                knowledgeMap = GetComponent<KnowledgeMap>();

            if (pathValidator == null)
                pathValidator = GetComponent<PathValidator>();
        }

        public ExplorationNode BestFrontierNode { get; private set; }

        public ExplorationNode EvaluateNextFrontier(Vector3 currentPosition)
        {
            if (knowledgeMap == null || knowledgeMap.AllNodes == null)
                return null;

            List<ExplorationNode> frontiers = GetFrontierNodes();

            if (frontiers.Count == 0)
            {
                BestFrontierNode = null;
                return null;
            }

            ExplorationNode bestNode = null;
            float highestScore = float.MinValue;
            NavMeshPath path = new NavMeshPath();

            foreach (var node in frontiers)
            {
                if (pathValidator != null &&
                    !pathValidator.IsPathFullyExplored(currentPosition, node.Position))
                {
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
                    continue;

                // Skor denklemi — mevcut mantık korundu
                float score = (unknownNeighbors * infoGainWeight)
                            - (pathDistance * distanceWeight)
                            - (node.VisitCount * visitPenaltyWeight);

                if (score > highestScore)
                {
                    highestScore = score;
                    bestNode = node;
                }
            }

            
            if (bestNode == null)
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

            foreach (var node in knowledgeMap.AllNodes)
            {
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
                            break;
                        }
                    }
                }
            }

            return list;
        }

        private float GetPathLength(NavMeshPath path)
        {
            float len = 0f;

            for (int i = 1; i < path.corners.Length; i++)
            {
                len += Vector3.Distance(
                    path.corners[i - 1],
                    path.corners[i]
                );
            }

            return len;
        }
    }
}