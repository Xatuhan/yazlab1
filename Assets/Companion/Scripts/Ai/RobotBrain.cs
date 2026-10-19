using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotBrain : MonoBehaviour, IPetBrain
{
    [Header("Takip Ayarlarý")]
    [SerializeField] private Transform target;
    [SerializeField] private float followDistance = 2.5f;

    public NavMeshAgent Agent { get; private set; }
    public Transform Transform => transform;
    public Transform Target => target;
    public float FollowDistance => followDistance;
    public PetStateMachine StateMachine { get; private set; }

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        StateMachine = new PetStateMachine();
    }

    private void Start()
    {
        StateMachine.Initialize(new IdleState(this));
    }

    private void Update()
    {
        StateMachine.Update();
    }
}