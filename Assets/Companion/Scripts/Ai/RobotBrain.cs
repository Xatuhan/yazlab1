
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotBrain : MonoBehaviour, IPetBrain
{
    [Header("Takip Ayarları")]
    [SerializeField] private Transform target;
    [SerializeField] private float followDistance = 2.5f;

    [Header("Lazer Öğrenme")]
    [SerializeField] private float resetCooldown = 1f;
    [SerializeField] private int observationsToLearn = 3;

    public NavMeshAgent Agent { get; private set; }
    public Transform Transform => transform;
    public Transform Target => target;
    public float FollowDistance => followDistance;
    public PetStateMachine StateMachine { get; private set; }

    public int LaserHitCount { get; private set; }
    public int LaserObservationCount { get; private set; }
    public bool HasEnoughLaserObservations =>
        LaserObservationCount >= observationsToLearn;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float nextLaserResetTime;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        StateMachine = new PetStateMachine();
    }

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        StateMachine.Initialize(new IdleState(this));
    }

    private void Update()
    {
        StateMachine.Update();
    }

    public void ResetToStartPosition()
    {
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.ResetPath();
            Agent.Warp(startPosition);
        }
        else
        {
            transform.position = startPosition;
        }

        transform.rotation = startRotation;

        if (Agent != null && Agent.isOnNavMesh)
            Agent.isStopped = false;

        StateMachine.ChangeState(new MoveToTargetState(this));

        Debug.Log(
            "<color=yellow>[RobotBrain] Robot başlangıca sıfırlandı.</color>"
        );
    }

    // Gerçek bir lazer çarpışması gerçekleştiğinde çağrılır.
    public void HandleLaserHit()
    {
        if (Time.time < nextLaserResetTime)
            return;

        nextLaserResetTime = Time.time + resetCooldown;

        LaserHitCount++;
        LaserObservationCount++;

        Debug.Log(
            $"[Laser Learning] Çarpışma: {LaserHitCount} | " +
            $"Gözlem: {LaserObservationCount}/{observationsToLearn} | " +
            $"Yeterli gözlem: {HasEnoughLaserObservations}"
        );

        ResetToStartPosition();
    }

    // İleride algı sistemi tarafından lazerin durum değişimi
    // gözlemlendiğinde kullanılacak kayıt noktası.
    public void RecordLaserObservation(bool laserActive)
    {
        LaserObservationCount++;

        Debug.Log(
            $"[Laser Learning] Lazer gözlemi: " +
            $"{(laserActive ? "AÇIK" : "KAPALI")} | " +
            $"Toplam gözlem: {LaserObservationCount}"
        );
    }

    private void OnDrawGizmos()
    {
        if (Agent == null || !Agent.hasPath)
            return;

        Gizmos.color = Color.yellow;
        NavMeshPath path = Agent.path;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Gizmos.DrawLine(
                path.corners[i] + Vector3.up * 0.2f,
                path.corners[i + 1] + Vector3.up * 0.2f
            );
        }
    }
}
