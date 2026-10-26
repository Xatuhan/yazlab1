using UnityEngine;
using UnityEngine.AI;

namespace AI.Core
{
    public class PathValidator : MonoBehaviour
    {
        [SerializeField] private KnowledgeMap knowledgeMap;

        /// <summary>
        /// Rota üzerindeki tüm köþeler bilinen (Seen/Visited) alanda mý kontrol eder.
        /// </summary>

        public bool IsPathFullyExplored(Vector3 start, Vector3 target)
        {
            NavMeshPath path = new NavMeshPath();

            if (!NavMesh.CalculatePath(start, target, NavMesh.AllAreas, path))
                return false;

            return path.status == NavMeshPathStatus.PathComplete;
        }


        private bool IsKnown(Vector3 point)
        {
            ExplorationNode nearest = GetNearestNode(point);
            return nearest != null && nearest.State != NodeState.Unknown;
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
                float distanceSqr = (point - node.Position).sqrMagnitude;

                if (distanceSqr < minDistSqr)
                {
                    minDistSqr = distanceSqr;
                    closest = node;
                }
            }

            // Düðüm aralýðýna göre daha sýký eþleþtirme.
            float tolerance = Mathf.Max(0.5f, knowledgeMap.NodeSpacing * 0.75f);

            return closest != null && minDistSqr <= tolerance * tolerance
                ? closest
                : null;
        }
    }
}