using UnityEngine;

public class MoveToTargetState : IPetState
{
    private readonly IPetBrain brain;

    public MoveToTargetState(IPetBrain brain)
    {
        this.brain = brain;
    }

    public void Enter() { }

    public void Update()
    {
        if (brain.Target == null) return;

        float distance = Vector3.Distance(brain.Transform.position, brain.Target.position);

        if (distance <= brain.FollowDistance)
        {
            brain.StateMachine.ChangeState(new IdleState(brain));
            return;
        }

        brain.Agent.SetDestination(brain.Target.position);
    }

    public void Exit()
    {
        if (brain.Agent.hasPath)
        {
            brain.Agent.ResetPath();
        }
    }
}