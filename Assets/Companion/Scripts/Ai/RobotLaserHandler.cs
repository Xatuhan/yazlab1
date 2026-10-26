using UnityEngine;

namespace AI
{
    [RequireComponent(typeof(RobotBrain))]
    public class RobotLaserHandler : MonoBehaviour
    {
        private RobotBrain brain;

        private void Awake() { brain = GetComponent<RobotBrain>(); }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Laser") || other.name.Contains("Laser"))
            {
                brain.HandleLaserHit();
            }
        }
    }
}
