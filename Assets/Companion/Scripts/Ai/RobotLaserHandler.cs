
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
            bool isLaser = other.CompareTag("Laser");

            Debug.Log(
                $"[ROBOT LASER DEBUG] Object={other.name}, " +
                $"Tag={other.tag}, IsLaser={isLaser}"
            );

            if (!isLaser)
                return;

            Debug.LogError(
                $"[ROBOT LASER HIT] {other.name}",
                other.gameObject
            );

            brain.HandleLaserHit();
        }
    }
}
