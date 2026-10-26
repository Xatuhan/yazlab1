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
            if (!NavMesh.CalculatePath(start, target, NavMesh.AllAreas, path)) return false;
            if (path.status != NavMeshPathStatus.PathComplete) return false;

            float spacing = knowledgeMap != null ? Mathf.Max(0.25f, knowledgeMap.NodeSpacing * 0.5f) : 0.5f;
            for (int i = 0; i < path.corners.Length; i++)
            {
                Vector3 from = path.corners[i];
                if (!IsKnown(from)) return false;
                if (i == path.corners.Length - 1) continue;

                Vector3 to = path.corners[i + 1];
                float distance = Vector3.Distance(from, to);
                int samples = Mathf.CeilToInt(distance / spacing);
                for (int sample = 1; sample < samples; sample++)
                {
                    Vector3 point = Vector3.Lerp(from, to, sample / (float)samples);
                    if (!IsKnown(point)) return false;
                }
            }
            return true;
        }

        private bool IsKnown(Vector3 point)
        {
            ExplorationNode nearest = GetNearestNode(point);
            return nearest != null && nearest.State != NodeState.Unknown;
        }

        private ExplorationNode GetNearestNode(Vector3 point)
        {
            if (knowledgeMap == null) return null;

            ExplorationNode closest = null;
            float minDist = float.MaxValue;

            foreach (var node in knowledgeMap.AllNodes)
            {
                float d = Vector3.Distance(point, node.Position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = node;
                }
            }

            return minDist <= 3.0f ? closest : null;
        }
    }
}