
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
            bool isLaser = other.gameObject.layer == LayerMask.NameToLayer("Laser");

            if (!isLaser)
                return;

            Debug.Log(
                $"[ROBOT LASER HIT] {other.name}",
                other.gameObject
            );

            brain.HandleLaserHit();
        }
    }
}
