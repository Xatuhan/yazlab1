using AI.States;
using System.Xml;
using UnityEngine;

namespace AI
{
    [RequireComponent(typeof(RobotBrain))]
    public class RobotLaserHandler : MonoBehaviour
    {
        private RobotBrain brain;

        private void Awake()
        {
            brain = GetComponent<RobotBrain>();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Deðdiðimiz obje bir lazer mi kontrol ediyoruz (Tag veya IsTrigger kontrolü)
            if (other.CompareTag("Laser") || other.name.Contains("Laser"))
            {
                // Robot zaten ölü veya reaksiyon durumunda deðilse duruma geç
                // RobotLaserHandler.cs içindeki tetiklenme aný:
                if (!(brain.StateMachine.CurrentState is PetDeadState))
                {
                    brain.StateMachine.ChangeState(new PetDeadState(brain));
                }
            }
        }
    }
}