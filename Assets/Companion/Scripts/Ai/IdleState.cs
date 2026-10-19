using UnityEngine;

public class IdleState : IPetState
{
    private readonly IPetBrain brain;

    public IdleState(IPetBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        if (brain.Agent.hasPath)
        {
            brain.Agent.ResetPath();
        }
    }

    public void Update()
    {
        if (brain.Target == null) return;

        float distance = Vector3.Distance(brain.Transform.position, brain.Target.position);
        if (distance > brain.FollowDistance)
        {
            brain.StateMachine.ChangeState(new MoveToTargetState(brain));
        }
    }

    public void Exit() { }
}