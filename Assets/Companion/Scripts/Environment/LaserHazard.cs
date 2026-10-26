using UnityEngine;

public class LaserHazard : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        RobotBrain robot = other.GetComponentInParent<RobotBrain>();
        if (robot != null)
        {
            Debug.Log("<color=red>[Lazer Teması]: Robot lazere çarptı! Başlangıç konumuna sıfırlanıyor...</color>");
            robot.HandleLaserHit();
        }
    }
}
