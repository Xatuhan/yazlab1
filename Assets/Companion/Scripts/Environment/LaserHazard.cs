using UnityEngine;
using System.Collections.Generic;

public class LaserHazard : MonoBehaviour
{
    private readonly HashSet<RobotBrain> robotsInside =
        new HashSet<RobotBrain>();

    private void OnTriggerEnter(Collider other)
    {
        RobotBrain robot = other.GetComponentInParent<RobotBrain>();

        if (robot == null)
            return;

        // Aynı robotun birden fazla collider'ı
        // aynı lazer temasını tekrar tekrar saymasın.
        if (!robotsInside.Add(robot))
            return;

        Debug.Log(
            $"[LASER DEBUG] Laser: {name} | " +
            $"Object: {other.name} | " +
            $"RobotBrain found: True",
            other.gameObject
        );

        // Maze kontrolü hareketi devraldıysa
        // ORB sisteminin reset mekanizmasını çalıştırma.
        if (robot.ExternalControl)
        {
            Debug.Log("[LaserHazard] Maze kontrolü aktif. ORB reseti engellendi.");
            return;
        }

        robot.HandleLaserHit();
    }

    private void OnTriggerExit(Collider other)
    {
        RobotBrain robot = other.GetComponentInParent<RobotBrain>();

        if (robot != null)
            robotsInside.Remove(robot);
    }
}