
using UnityEngine;

public class LaserHazard : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.LogError("[TEST 123] YENİ LASERHAZARD KODU ÇALIŞIYOR");

        RobotBrain robot = other.GetComponentInParent<RobotBrain>();

        Debug.Log(
            $"[LASER DEBUG] Laser: {name} | " +
            $"Object: {other.name} | " +
            $"Collider: {other.GetType().Name} | " +
            $"Center: {other.bounds.center} | " +
            $"Size: {other.bounds.size} | " +
            $"RobotBrain found: {robot != null}",
            other.gameObject
        );

        if (robot != null)
        {
            robot.HandleLaserHit();
        }
    }
}
