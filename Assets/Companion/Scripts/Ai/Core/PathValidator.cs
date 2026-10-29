
using UnityEngine;
using UnityEngine.AI;

namespace AI.Core
{
    public class PathValidator : MonoBehaviour
    {
        [SerializeField] private KnowledgeMap knowledgeMap;

        private void Awake()
        {
            if (knowledgeMap == null)
                knowledgeMap = GetComponent<KnowledgeMap>();
        }

        public bool IsPathFullyExplored(Vector3 start, Vector3 target)
        {
            if (knowledgeMap == null ||
                knowledgeMap.AllNodes == null ||
                knowledgeMap.AllNodes.Count == 0)
            {
                Debug.LogWarning(
                    "[PathValidator] KnowledgeMap veya düðümler bulunamadý."
                );
                return false;
            }

            NavMeshPath path = new NavMeshPath();

            if (!NavMesh.CalculatePath(
                    start,
                    target,
                    NavMesh.AllAreas,
                    path))
            {
                Debug.LogWarning(
                    $"[PathValidator] Rota hesaplanamadý: {target}"
                );
                return false;
            }

            if (path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning(
                    $"[PathValidator] Rota tamamlanmadý: {target} | " +
                    $"Durum: {path.status}"
                );
                return false;
            }

            // Hedef düðüm bilinmiyorsa hedefi geçerli sayma.
            if (!IsKnown(target))
            {
                Debug.LogWarning(
                    $"[PathValidator] Hedef bilinen bir düðüm deðil: {target}"
                );
                return false;
            }

            // Ara noktalarý kontrol etmiyoruz.
            // Böylece havalandýrmaya giden geçerli NavMesh rotasý
            // ara nokta eþleþtirmesi yüzünden reddedilmez.
            return true;
        }

        private bool IsKnown(Vector3 point)
        {
            ExplorationNode nearest = GetNearestNode(point);

            return nearest != null &&
                   nearest.State != NodeState.Unknown;
        }

        private ExplorationNode GetNearestNode(Vector3 point)
        {
            if (knowledgeMap == null ||
                knowledgeMap.AllNodes == null ||
                knowledgeMap.AllNodes.Count == 0)
            {
                return null;
            }

            ExplorationNode closest = null;
            float minDistSqr = float.MaxValue;

            foreach (var node in knowledgeMap.AllNodes)
            {
                float distanceSqr =
                    (point - node.Position).sqrMagnitude;

                if (distanceSqr < minDistSqr)
                {
                    minDistSqr = distanceSqr;
                    closest = node;
                }
            }

            float tolerance = Mathf.Max(
                0.5f,
                knowledgeMap.NodeSpacing * 0.75f
            );

            return closest != null &&
                   minDistSqr <= tolerance * tolerance
                ? closest
                : null;
        }
    }
}
