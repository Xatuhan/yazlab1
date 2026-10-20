using UnityEngine;
using UnityEngine.AI;

public interface IPetBrain
{
    NavMeshAgent Agent { get; }
    Transform Transform { get; }
    Transform Target { get; }
    PetStateMachine StateMachine { get; }
    float FollowDistance { get; }
}