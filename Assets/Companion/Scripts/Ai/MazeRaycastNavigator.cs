
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(RobotBrain))]
[RequireComponent(typeof(MazeKnowledge))]
public class MazeRaycastNavigator : MonoBehaviour
{
    [Header("Şalter Algılama")]
    [SerializeField] private float terminalDetectionDistance = 1.5f;
    [SerializeField] private float terminalCheckInterval = 0.2f;

    private float nextTerminalCheckTime;

    [Header("Katmanlar")]
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask laserMask;

    [Header("Maze Giriş Ayarları")]
    [SerializeField] private Transform mazeEntryPoint;
    [SerializeField] private float entryStopDistance = 0.3f;

    private bool movingToEntry;

    [Header("Tarama")]
    [SerializeField] private float scanHeight = 0.3f;
    [SerializeField] private float scanDistance = 4f;
    [SerializeField] private float rayRadius = 0.25f;
    [SerializeField] private float angleStep = 30f;
    [SerializeField] private float decisionInterval = 0.2f;

    [Header("Güvenlik / Puanlama")]
    [SerializeField] private float laserSafeDistance = 1.5f;
    [SerializeField] private float wallMinDistance = 0.6f;
    [SerializeField] private float forwardBonus = 1.0f;
    [SerializeField] private float visitPenalty = 1.5f;

    [Header("Hareket")]
    [SerializeField] private float turnSpeed = 240f;

    public bool IsActive { get; private set; }

    private RobotBrain brain;
    private MazeKnowledge mazeKnowledge;

    private Vector3 moveDir;
    private float nextDecisionTime;

    private Vector2Int lastRegisteredCell;
    private bool hasRegisteredCell;

    private void Awake()
    {
        brain = GetComponent<RobotBrain>();
        mazeKnowledge = GetComponent<MazeKnowledge>();
    }

    public void StartMazeMode()
    {
        if (IsActive) return;

        if (mazeEntryPoint == null)
        {
            Debug.LogError(
                "[MazeRaycast] MazeEntryPoint atanmamış!"
            );
            return;
        }

        if (brain.Agent == null || !brain.Agent.isOnNavMesh)
        {
            Debug.LogError(
                "[MazeRaycast] Robot NavMesh üzerinde değil!"
            );
            return;
        }

        mazeKnowledge.ClearMap();

        hasRegisteredCell = false;
        nextDecisionTime = 0f;
        nextTerminalCheckTime = 0f;
        movingToEntry = true;

        SetActive(true);

        if (!brain.Agent.SetDestination(mazeEntryPoint.position))
        {
            Debug.LogError(
                "[MazeRaycast] Giriş noktasına rota oluşturulamadı!"
            );

            movingToEntry = false;
            SetActive(false);
            return;
        }

    }

    public void EndMazeMode()
    {
        SetActive(false);
    }

    private void OnDisable()
    {
        SetActive(false);
    }

    private void SetActive(bool value)
    {
        if (IsActive == value)
            return;

        IsActive = value;
        brain.ExternalControl = value;

        if (brain.Agent != null && brain.Agent.isOnNavMesh)
        {
            brain.Agent.isStopped = false;
            brain.Agent.ResetPath();
        }

        if (value)
        {
            hasRegisteredCell = false;
            nextDecisionTime = 0f;
            moveDir = transform.forward;
        }
        else
        {
            movingToEntry = false;
            moveDir = Vector3.zero;
        }

        
    }

    private void Update()
    {
        if (!IsActive ||
            brain.Agent == null ||
            !brain.Agent.isOnNavMesh)
            return;

        // AŞAMA 1: Maze giriş noktasına git.
        if (movingToEntry)
        {
            if (brain.Agent.pathPending)
                return;

            if (!brain.Agent.hasPath ||
                brain.Agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning(
                    "[MazeRaycast] MazeEntryPoint'e tam rota bulunamadı!"
                );

                movingToEntry = false;
                SetActive(false);
                return;
            }

            if (brain.Agent.remainingDistance <= entryStopDistance)
            {
                brain.Agent.ResetPath();

                movingToEntry = false;
                hasRegisteredCell = false;
                nextDecisionTime = 0f;
                nextTerminalCheckTime = 0f;
                moveDir = transform.forward;

               
            }

            return;
        }

        // AŞAMA 2: Yakındaki şalterleri kontrol et.
        FindNearbyTerminal();

        // Şalter algılandıysa kontrol başka state'e devredilmiştir.
        if (!IsActive)
            return;

        // AŞAMA 3: Mevcut raycast keşif sistemi.
        RegisterVisit();

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + decisionInterval;
            moveDir = ChooseDirection();

            
        }

        if (moveDir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot =
            Quaternion.LookRotation(moveDir, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            turnSpeed * Time.deltaTime
        );

        float angle = Vector3.Angle(
            transform.forward,
            moveDir
        );

        if (angle < 45f)
        {
            brain.Agent.Move(
                moveDir * brain.Agent.speed * Time.deltaTime
            );
        }
    }

    private void FindNearbyTerminal()
    {
        if (Time.time < nextTerminalCheckTime)
            return;

        nextTerminalCheckTime =
            Time.time + terminalCheckInterval;

        DoorTerminal nearest = null;
        float nearestDistance = terminalDetectionDistance;

        foreach (DoorTerminal terminal in DoorTerminal.All)
        {
            if (terminal == null || terminal.IsUsed)
                continue;

            Vector3 terminalPosition = terminal.transform.position;

            if (!mazeKnowledge.IsInsideMaze(terminalPosition))
                continue;

            float distance = Vector3.Distance(
                transform.position,
                terminalPosition
            );

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearest = terminal;
            }
        }

        if (nearest == null)
            return;

        

        // Önce Maze navigasyonunu kapat.
        // Böylece kendi hareket kontrolü Agent rotasını ezmez.
        SetActive(false);

        // Mevcut şalter yaklaşma ve etkileşim sistemine devret.
        brain.StateMachine.ChangeState(
            new AI.States.PetInvestigateState(brain, nearest)
        );

        Debug.Log(
            $"[MazeRaycast] Kontrol PetInvestigateState'e devredildi: {nearest.name}"
        );
    }

    private Vector3 ChooseDirection()
    {
       
        Vector3 origin =
            transform.position + Vector3.up * scanHeight;

        int combined = wallMask.value | laserMask.value;

        float bestScore = float.NegativeInfinity;
        Vector3 bestDir = Vector3.zero;

        for (float a = -180f; a < 180f; a += angleStep)
        {
            Vector3 dir =
                Quaternion.Euler(0f, a, 0f) * transform.forward;

            Vector3 nextPosition =
                transform.position + dir * 1.5f;

            if (!mazeKnowledge.IsInsideMaze(nextPosition))
            {
                
                continue;
            }

            float clear = scanDistance;
            bool laserHit = false;
            bool wallHit = false;

            RaycastHit hit = default;

            if (Physics.SphereCast(
                origin,
                rayRadius,
                dir,
                out hit,
                scanDistance,
                combined,
                QueryTriggerInteraction.Collide))
            {
                clear = hit.distance;

                laserHit = IsInMask(
                    hit.collider.gameObject.layer,
                    laserMask
                );

                

                wallHit = IsInMask(
                    hit.collider.gameObject.layer,
                    wallMask
                );
            }

            mazeKnowledge.ObserveClearRay(
                origin,
                dir,
                clear
            );

            if (wallHit)
            {
                

                mazeKnowledge.ObserveWall(hit.point);
            }

            if (laserHit && clear < laserSafeDistance)
            {
                
                continue;
            }

            if (wallHit && clear < wallMinDistance)
            {
                
                continue;
            }

            float score = clear;

            if (laserHit)
                score -= 2f;

            score +=
                Mathf.Cos(a * Mathf.Deg2Rad) * forwardBonus;

            score -= mazeKnowledge.GetVisitCount(
                mazeKnowledge.WorldToCell(nextPosition)
            ) * visitPenalty;

            

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }

        

        if (bestDir == Vector3.zero)
        {
            Debug.LogWarning(
                "[MazeRaycast] Maze sınırları içinde güvenli yön bulunamadı!"
            );

            return Vector3.zero;
        }

      

        return bestDir;
    }

    private static bool IsInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void RegisterVisit()
    {
        Vector2Int currentCell =
            mazeKnowledge.WorldToCell(transform.position);

        if (hasRegisteredCell &&
            currentCell == lastRegisteredCell)
            return;

        mazeKnowledge.RegisterVisit(transform.position);

        lastRegisteredCell = currentCell;
        hasRegisteredCell = true;
    }
}
