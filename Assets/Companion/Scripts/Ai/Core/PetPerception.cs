using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Core
{
    public class PetPerception : MonoBehaviour
    {
        [Header("Görüş ve Mesafe Ayarları")]
        [SerializeField] private Transform eyeTransform;
        [SerializeField] private float viewDistance = 18.0f;
        [SerializeField] private float fieldOfViewAngle = 180.0f;

        [Header("Katman (Layer) Filtreleri")]
        [Tooltip("Sadece duvarlar ve görüşü kapatan statik engeller")]
        [SerializeField] private LayerMask visionObstacleMask;

        [Tooltip("Sadece Lazer nesnelerinin bulunduğu özel katman")]
        [SerializeField] private LayerMask laserMask;

        [Header("Referanslar")]
        [SerializeField] private KnowledgeMap knowledgeMap;

        public DoorTerminal DetectedTerminal { get; private set; }

        private Collider[] selfColliders;
        private readonly HashSet<Collider> loggedLaserBlockers = new HashSet<Collider>();

        private void Awake()
        {
            if (knowledgeMap == null)
            {
                knowledgeMap = GetComponent<KnowledgeMap>();
            }
            // Robotun kendi collider'larını kaydet (Kendini lazer/engel sanmasın)
            selfColliders = GetComponentsInChildren<Collider>();
        }

        private void Update()
        {
            if (knowledgeMap == null || knowledgeMap.AllNodes == null || knowledgeMap.AllNodes.Count == 0) return;

            UpdateNodeVisibility();
            DetectedTerminal = ScanForTerminals();
        }

        public void ForceInitialScan()
        {
            if (knowledgeMap == null || knowledgeMap.AllNodes == null || knowledgeMap.AllNodes.Count == 0) return;

            ExplorationNode startNode = GetNearestNode(transform.position);
            if (startNode != null)
            {
                startNode.State = NodeState.Visited;
                RevealNeighborNodes(startNode);
            }

            UpdateNodeVisibility(fullCircle: true);
            DetectedTerminal = ScanForTerminals();
        }

        private ExplorationNode GetNearestNode(Vector3 point)
        {
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

            return closest;
        }

        public void UpdateNodeVisibility(bool fullCircle = false)
        {
            // Tünellerde tavan çarpmasını önlemek için 0.2m göz yüksekliği
            Vector3 eyePos = eyeTransform != null ? eyeTransform.position : transform.position + Vector3.up * 0.2f;

            foreach (var node in knowledgeMap.AllNodes)
            {
                if (node.State == NodeState.Visited || node.State == NodeState.Checked) continue;

                Vector3 dirToNode = (node.Position + Vector3.up * 0.2f) - eyePos;
                float dist = dirToNode.magnitude;

                if (dist <= viewDistance)
                {
                    float angle = Vector3.Angle(transform.forward, dirToNode);
                    if (fullCircle || angle <= fieldOfViewAngle * 0.5f)
                    {
                        // 1. Görüş duvar/engel tarafından kesiliyor mu?
                        bool isBlockedByWall = Physics.Raycast(eyePos, dirToNode.normalized, dist, visionObstacleMask, QueryTriggerInteraction.Ignore);

                        // 2. Araya bir Lazer Trigger'ı giriyor mu?
                        bool isBlockedByLaser = Physics.Raycast(eyePos, dirToNode.normalized, out RaycastHit laserHit, dist, laserMask, QueryTriggerInteraction.Collide);

                        if (isBlockedByLaser && loggedLaserBlockers.Add(laserHit.collider))
                        {
                            Debug.Log($"[Perception Log]: Lazer engeline takıldı -> Obj: {laserHit.collider.name}, Layer: {LayerMask.LayerToName(laserHit.collider.gameObject.layer)}");
                        }

                        if (!isBlockedByWall && !isBlockedByLaser)
                        {
                            node.State = NodeState.Seen;
                        }
                    }
                }
            }

            // Robotun üzerinde durduğu düğümün açık NavMesh komşularını otomatik aç (Tünel geçişleri için)
            ExplorationNode currentNode = GetNearestNode(transform.position);
            if (currentNode != null && Vector3.Distance(transform.position, currentNode.Position) <= 1.2f)
            {
                RevealNeighborNodes(currentNode);
            }
        }

        /// <summary>
        /// Dar havalandırma tünellerinde Raycast tavan veya duvar açısına takılsa bile 
        /// NavMesh üzerinde doğrudan yürünebilir komşu düğümleri Seen (Yeşil) yapar.
        /// </summary>
        private void RevealNeighborNodes(ExplorationNode node)
        {
            if (node == null || node.Neighbors == null) return;

            NavMeshPath path = new NavMeshPath();
            foreach (var neighbor in node.Neighbors)
            {
                if (neighbor.State == NodeState.Unknown)
                {
                    if (NavMesh.CalculatePath(node.Position, neighbor.Position, NavMesh.AllAreas, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            neighbor.State = NodeState.Seen;
                        }
                    }
                }
            }
        }

        public DoorTerminal FindVisibleTerminalForDoor(Door requiredDoor)
        {
            return ScanForTerminals(requiredDoor);
        }

        private DoorTerminal ScanForTerminals(Door requiredDoor = null)
        {
            Vector3 eyePos = eyeTransform != null ? eyeTransform.position : transform.position + Vector3.up * 0.3f;
            Collider[] hits = Physics.OverlapSphere(transform.position, viewDistance);

            foreach (var hit in hits)
            {
                if (IsSelfCollider(hit)) continue;

                DoorTerminal terminal = hit.GetComponentInParent<DoorTerminal>();
                if (terminal == null || terminal.IsUsed) continue;
                if (requiredDoor != null && terminal.TargetDoor != requiredDoor) continue;

                Vector3 targetCenter = terminal.transform.position + Vector3.up * 0.5f;
                Vector3 dir = targetCenter - eyePos;
                float dist = dir.magnitude;
                if (dist <= 0f || dist > viewDistance) continue;
                if (Vector3.Angle(transform.forward, dir) > fieldOfViewAngle * 0.5f) continue;

                if (Physics.Raycast(eyePos, dir.normalized, out RaycastHit rayHit, dist,
                    visionObstacleMask, QueryTriggerInteraction.Ignore))
                {
                    if (rayHit.transform != terminal.transform && !rayHit.transform.IsChildOf(terminal.transform))
                        continue;
                }

                return terminal;
            }

            return null;
        }

        private bool IsSelfCollider(Collider col)
        {
            if (selfColliders == null) return false;
            foreach (var self in selfColliders)
            {
                if (self == col) return true;
            }
            return false;
        }

        public void ClearDetectedTerminal()
        {
            DetectedTerminal = null;
        }
    }
}