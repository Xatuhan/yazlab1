using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotBrain : MonoBehaviour, IPetBrain
{
    [Header("Takip Ayarları")]
    [SerializeField] private Transform target;
    [SerializeField] private float followDistance = 2.5f;

    public NavMeshAgent Agent { get; private set; }
    public Transform Transform => transform;
    public Transform Target => target;
    public float FollowDistance => followDistance;
    public PetStateMachine StateMachine { get; private set; }

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

    private void Update() => StateMachine.Update();

    public void ResetToStartPosition()
    {
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.ResetPath();
            Agent.Warp(startPosition);
        }
        else transform.position = startPosition;

        transform.rotation = startRotation;
        StateMachine.ChangeState(new MoveToTargetState(this));
        Debug.Log("<color=yellow>[RobotBrain]: Robot lazere çarptı ve başlangıç noktasına sıfırlandı!</color>");
    }

    // Shared laser response; suppresses duplicate callbacks from overlapping beams.
    public void HandleLaserHit()
    {
        if (Time.time < nextLaserResetTime) return;
        nextLaserResetTime = Time.time + 1f;
        ResetToStartPosition();
    }

    private void OnDrawGizmos()
    {
        if (Agent == null || !Agent.hasPath) return;
        Gizmos.color = Color.yellow;
        NavMeshPath path = Agent.path;
        for (int i = 0; i < path.corners.Length - 1; i++)
            Gizmos.DrawLine(path.corners[i] + Vector3.up * 0.2f, path.corners[i + 1] + Vector3.up * 0.2f);
    }
}
