
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(RobotBrain))]
[RequireComponent(typeof(MazeKnowledge))]
public class MazeRaycastNavigator : MonoBehaviour
{
    [Header("Katmanlar")]
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask laserMask;

    [Header("Maze Giriþ Ayarlarý")]
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
                "[MazeRaycast] MazeEntryPoint atanmamýþ!"
            );
            return;
        }

        if (brain.Agent == null || !brain.Agent.isOnNavMesh)
        {
            Debug.LogError(
                "[MazeRaycast] Robot NavMesh üzerinde deðil!"
            );
            return;
        }

        mazeKnowledge.ClearMap();

        hasRegisteredCell = false;
        nextDecisionTime = 0f;
        movingToEntry = true;

        SetActive(true);

        if (!brain.Agent.SetDestination(mazeEntryPoint.position))
        {
            Debug.LogError(
                "[MazeRaycast] Giriþ noktasýna rota oluþturulamadý!"
            );

            movingToEntry = false;
            SetActive(false);
            return;
        }

        Debug.Log(
            "[MazeRaycast] Maze giriþ noktasýna gidiliyor."
        );
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

        Debug.Log(
            $"[MazeRaycast] Mod: {(value ? "AÇIK" : "KAPALI")}"
        );
    }

    private void Update()
    {
        if (!IsActive ||
            brain.Agent == null ||
            !brain.Agent.isOnNavMesh)
            return;

        // AÞAMA 1: Maze giriþ noktasýna git.
        if (movingToEntry)
        {
            if (brain.Agent.pathPending)
                return;

            if (!brain.Agent.hasPath ||
                brain.Agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning(
                    "[MazeRaycast] MazeEntryPoint'e tam rota bulunamadý!"
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
                moveDir = transform.forward;

                Debug.Log(
                    "[MazeRaycast] Giriþe ulaþýldý. Keþif baþlýyor."
                );
            }

            return;
        }

        // AÞAMA 2: Mevcut raycast keþif sistemi.
        RegisterVisit();

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + decisionInterval;
            moveDir = ChooseDirection();

            Debug.Log(
                $"[MazeRaycast] Seçilen yön: {moveDir}"
            );
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

    private Vector3 ChooseDirection()
    {
        int outsideBoundsCount = 0;
        int laserBlockedCount = 0;
        int wallBlockedCount = 0;
        int acceptedCount = 0;
        Vector3 origin =
            transform.position + Vector3.up * scanHeight;

        int combined = wallMask.value | laserMask.value;

        float bestScore = float.NegativeInfinity;
        Vector3 bestDir = Vector3.zero;

        for (float a = -180f; a < 180f; a += angleStep)
        {
            Vector3 dir =
                Quaternion.Euler(0f, a, 0f) * transform.forward;

            // Bir sonraki hedef hücre Maze dýþýnda kalýyorsa
            // bu yönü deðerlendirme.
            Vector3 nextPosition =
                transform.position + dir * 1.5f;

            if (!mazeKnowledge.IsInsideMaze(nextPosition))
            {
                outsideBoundsCount++;
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
                if (laserHit)
                {
                    Debug.Log(
                        $"[MAZE LASER DEBUG] " +
                        $"Collider: {hit.collider.name} | " +
                        $"Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)} | " +
                        $"Mesafe: {hit.distance:F2} | " +
                        $"Hit Point: {hit.point}"
                    );
                }

                wallHit = IsInMask(
                    hit.collider.gameObject.layer,
                    wallMask
                );
            }

            // Yalnýzca Maze sýnýrlarý içindeki boþ alanlarý kaydet.
            mazeKnowledge.ObserveClearRay(
                origin,
                dir,
                clear
            );

            // Duvarlarý kaydet.
            if (wallHit)
            {
                Debug.Log(
                    $"[MAZE WALL DEBUG] Nesne: {hit.collider.name} | " +
                    $"Konum: {hit.point} | Mesafe: {hit.distance:F2}"
                );

                mazeKnowledge.ObserveWall(hit.point);
            }

            // Tehlikeli lazer yönlerini ele.
            if (laserHit && clear < laserSafeDistance)
            {
                laserBlockedCount++;
                continue;
            }

            // Duvara fazla yakýn yönleri ele.
            if (wallHit && clear < wallMinDistance)
            {
                wallBlockedCount++;
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
            acceptedCount++;
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }

        // Teþhis sonuçlarýný her durumda yazdýr.
        Debug.Log(
            $"[MAZE DIAGNOSTIC] " +
            $"Bounds dýþý: {outsideBoundsCount} | " +
            $"Lazer engeli: {laserBlockedCount} | " +
            $"Duvar engeli: {wallBlockedCount} | " +
            $"Kabul edilen: {acceptedCount}"
        );

        if (bestDir == Vector3.zero)
        {
            Debug.LogWarning(
                "[MazeRaycast] Maze sýnýrlarý içinde güvenli yön bulunamadý!"
            );

            return Vector3.zero;
        }

        Debug.Log(
            $"[MazeRaycast] Seçilen yön: {bestDir} | " +
            $"En iyi skor: {bestScore:F2}"
        );

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
