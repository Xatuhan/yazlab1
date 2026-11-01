
using System.Collections.Generic;
using UnityEngine;

namespace AI.Core
{
    public class PetPerception : MonoBehaviour
    {
        [Header("Görüş ve Mesafe Ayarları")]
        [SerializeField] private Transform eyeTransform;
        [SerializeField] private float viewDistance = 18.0f;
        [SerializeField] private float fieldOfViewAngle = 180.0f;

        [Tooltip("Labirent düğümlerinin keşfedilme mesafesi")]
        [SerializeField] private float mazeDiscoveryDistance = 2.0f;

        [Header("Katman (Layer) Filtreleri")]
        [Tooltip("Sadece duvarlar ve görüşü kapatan statik engeller")]
        [SerializeField] private LayerMask visionObstacleMask;

        [Tooltip("Sadece Lazer nesnelerinin bulunduğu özel katman")]
        [SerializeField] private LayerMask laserMask;

        [Tooltip("Lazer gözlem sensörlerinin bulunduğu Layer")]
        [SerializeField] private LayerMask laserSensorMask;

        [Header("Lazer Sensör Taraması")]
        [SerializeField] private float sensorScanInterval = 0.2f;
        [SerializeField] private float sensorMovementThreshold = 0.1f;

        [Header("Referanslar")]
        [SerializeField] private KnowledgeMap knowledgeMap;

        public DoorTerminal DetectedTerminal { get; private set; }

        private RobotBrain robotBrain;
        private Collider[] selfColliders;

        private readonly HashSet<Collider> loggedLaserBlockers =
            new HashSet<Collider>();

        private readonly HashSet<Collider> detectedSensors =
            new HashSet<Collider>();

        private readonly Dictionary<Collider, Vector3> sensorPositions =
            new Dictionary<Collider, Vector3>();

        private DoorTerminal lastLoggedTerminal;

        private float nextSensorScanTime;

        private bool MazeControlActive =>
            robotBrain != null && robotBrain.ExternalControl;

        private void Awake()
        {
            if (knowledgeMap == null)
                knowledgeMap = GetComponent<KnowledgeMap>();

            robotBrain = GetComponent<RobotBrain>();

            selfColliders = GetComponentsInChildren<Collider>();
        }

        private void Update()
        {
            // Lazer sensörleri Maze modunda da çalışmaya devam eder.
            ScanLaserSensors();

            // Maze kontrolü aktifken ORB terminal algılamasın.
            if (MazeControlActive)
            {
                DetectedTerminal = null;
                lastLoggedTerminal = null;
                return;
            }

            if (knowledgeMap == null ||
                knowledgeMap.AllNodes == null ||
                knowledgeMap.AllNodes.Count == 0)
                return;

            UpdateNodeVisibility();

            DoorTerminal detected = ScanForTerminals();

            if (detected != lastLoggedTerminal)
            {
                if (detected != null)
                {
                    Debug.Log(
                        $"[Perception Debug] Terminal algılandı: " +
                        $"{detected.name} | Mesafe: " +
                        $"{Vector3.Distance(transform.position, detected.transform.position):F2} m"
                    );
                }

                lastLoggedTerminal = detected;
            }

            DetectedTerminal = detected;
        }

        private void ScanLaserSensors()
        {
            if (Time.time < nextSensorScanTime)
                return;

            nextSensorScanTime =
                Time.time + sensorScanInterval;

            Vector3 eyePos = eyeTransform != null
                ? eyeTransform.position
                : transform.position + Vector3.up * 0.2f;

            Collider[] sensors = Physics.OverlapSphere(
                transform.position,
                viewDistance,
                laserSensorMask,
                QueryTriggerInteraction.Collide
            );

            foreach (Collider sensor in sensors)
            {
                if (sensor == null || IsSelfCollider(sensor))
                    continue;

                Vector3 target = sensor.bounds.center;
                Vector3 direction = target - eyePos;
                float distance = direction.magnitude;

                if (distance <= 0.001f)
                    continue;

                if (Vector3.Angle(
                    transform.forward,
                    direction) > fieldOfViewAngle * 0.5f)
                {
                    continue;
                }

                if (Physics.Raycast(
                    eyePos,
                    direction.normalized,
                    distance,
                    visionObstacleMask,
                    QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                Vector3 currentPosition = sensor.transform.position;

                if (detectedSensors.Add(sensor))
                {
                    sensorPositions[sensor] = currentPosition;

                    Debug.Log(
                        $"[Laser Sensor] Algılandı: {sensor.name} | " +
                        $"Konum: {currentPosition}"
                    );

                    continue;
                }

                if (!sensorPositions.TryGetValue(
                    sensor,
                    out Vector3 previousPosition))
                {
                    sensorPositions[sensor] = currentPosition;
                    continue;
                }

                float movement = Vector3.Distance(
                    previousPosition,
                    currentPosition
                );

                if (movement >= sensorMovementThreshold)
                {
                    Debug.Log(
                        $"[Laser Sensor] Hareket: {sensor.name} | " +
                        $"Eski: {previousPosition} | " +
                        $"Yeni: {currentPosition} | " +
                        $"Mesafe: {movement:F2} m"
                    );

                    sensorPositions[sensor] = currentPosition;
                }
            }
        }

        public void ForceInitialScan()
        {
            ScanLaserSensors();

            if (MazeControlActive)
            {
                DetectedTerminal = null;
                lastLoggedTerminal = null;
                return;
            }

            if (knowledgeMap == null ||
                knowledgeMap.AllNodes == null ||
                knowledgeMap.AllNodes.Count == 0)
                return;

            ExplorationNode startNode =
                GetNearestNode(transform.position);

            if (startNode != null)
                startNode.State = NodeState.Visited;

            UpdateNodeVisibility();

            DetectedTerminal = ScanForTerminals();
            lastLoggedTerminal = DetectedTerminal;

            if (DetectedTerminal != null)
            {
                Debug.Log(
                    $"[Perception Debug] İlk taramada terminal bulundu: " +
                    $"{DetectedTerminal.name}"
                );
            }
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
            if (knowledgeMap == null ||
                knowledgeMap.AllNodes == null)
                return;

            Vector3 eyePos = eyeTransform != null
                ? eyeTransform.position
                : transform.position + Vector3.up * 0.2f;

            foreach (var node in knowledgeMap.AllNodes)
            {
                if (node.State == NodeState.Visited ||
                    node.State == NodeState.Checked)
                    continue;

                Vector3 nodePos =
                    node.Position + Vector3.up * 0.2f;

                Vector3 dirToNode = nodePos - eyePos;
                float dist = dirToNode.magnitude;

                if (dist > mazeDiscoveryDistance)
                    continue;

                if (dist <= 0.001f)
                    continue;

                float angle = Vector3.Angle(
                    transform.forward,
                    dirToNode
                );

                if (!fullCircle &&
                    angle > fieldOfViewAngle * 0.5f)
                    continue;

                bool isBlockedByWall = Physics.Raycast(
                    eyePos,
                    dirToNode.normalized,
                    dist,
                    visionObstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                bool isBlockedByLaser = Physics.Raycast(
                    eyePos,
                    dirToNode.normalized,
                    out RaycastHit laserHit,
                    dist,
                    laserMask,
                    QueryTriggerInteraction.Collide
                );

                if (isBlockedByLaser &&
                    loggedLaserBlockers.Add(laserHit.collider))
                {
                    Debug.Log(
                        $"[Perception Log] Lazer engeli: " +
                        laserHit.collider.name +
                        " | Layer: " +
                        LayerMask.LayerToName(
                            laserHit.collider.gameObject.layer
                        )
                    );
                }

                if (!isBlockedByWall &&
                    !isBlockedByLaser &&
                    node.State == NodeState.Unknown)
                {
                    node.State = NodeState.Seen;
                }
            }
        }

        public DoorTerminal FindVisibleTerminalForDoor(
            Door requiredDoor)
        {
            if (MazeControlActive)
                return null;

            return ScanForTerminals(requiredDoor);
        }

        private DoorTerminal ScanForTerminals(
            Door requiredDoor = null)
        {
            if (MazeControlActive)
                return null;

            Vector3 eyePos = eyeTransform != null
                ? eyeTransform.position
                : transform.position + Vector3.up * 0.3f;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                viewDistance
            );

            foreach (var hit in hits)
            {
                if (IsSelfCollider(hit))
                    continue;

                DoorTerminal terminal =
                    hit.GetComponentInParent<DoorTerminal>();

                if (terminal == null || terminal.IsUsed)
                    continue;

                if (requiredDoor != null &&
                    terminal.TargetDoor != requiredDoor)
                    continue;

                float distToTerminal = Vector3.Distance(
                    transform.position,
                    terminal.transform.position
                );

                if (distToTerminal <= 3.0f)
                {
                    Debug.Log(
                        $"[Perception Debug] Yakın terminal bulundu: " +
                        $"{terminal.name} | Mesafe: {distToTerminal:F2} m"
                    );

                    return terminal;
                }

                Vector3 targetCenter =
                    terminal.transform.position + Vector3.up * 0.5f;

                Vector3 dir = targetCenter - eyePos;
                float dist = dir.magnitude;

                if (dist <= 0f || dist > viewDistance)
                    continue;

                if (Vector3.Angle(transform.forward, dir) >
                    fieldOfViewAngle * 0.5f)
                    continue;

                if (Physics.Raycast(
                    eyePos,
                    dir.normalized,
                    out RaycastHit rayHit,
                    dist,
                    visionObstacleMask,
                    QueryTriggerInteraction.Ignore))
                {
                    if (rayHit.transform != terminal.transform &&
                        !rayHit.transform.IsChildOf(
                            terminal.transform))
                    {
                        continue;
                    }
                }

                Debug.Log(
                    $"[Perception Debug] Görüş alanında terminal bulundu: " +
                    $"{terminal.name} | Mesafe: {distToTerminal:F2} m"
                );

                return terminal;
            }

            return null;
        }

        private bool IsSelfCollider(Collider col)
        {
            if (selfColliders == null)
                return false;

            foreach (var self in selfColliders)
            {
                if (self == col)
                    return true;
            }

            return false;
        }

        public void ClearDetectedTerminal()
        {
            DetectedTerminal = null;
            lastLoggedTerminal = null;
        }
    }
}
