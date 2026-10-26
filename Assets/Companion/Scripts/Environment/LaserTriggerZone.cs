
using UnityEngine;
using UnityEngine.InputSystem;

public class LaserTriggerZone : MonoBehaviour
{
    [Header("Bağlı Şalter/Terminal")]
    [Tooltip("Bu lazer alanının kapatması gereken özel şalteri buraya sürükleyin.")]
    [SerializeField] private DoorTerminal targetTerminal;

    private bool isPlayerInRange = false;
    private bool hasCommandedRobot = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;

            if (!hasCommandedRobot)
                Debug.Log("<color=white>[Lazer Geçidi]: Şalteri bulması için robota emret -> [E]</color>");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInRange = false;
    }

    private void Update()
    {
        bool isEPressed =
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;

        if (isPlayerInRange && isEPressed && !hasCommandedRobot)
            TriggerRobotToInvestigate();
    }

    private void TriggerRobotToInvestigate()
    {
        if (targetTerminal == null)
        {
            Debug.LogWarning("[Lazer Geçidi]: Target Terminal atanmamış!");
            return;
        }

        RobotBrain robot = FindAnyObjectByType<RobotBrain>();

        if (robot != null)
        {
            hasCommandedRobot = true;

            Debug.Log("<color=cyan>[Oyuncu]: Robota lazere bağlı özel şalteri açma emri verildi!</color>");
            robot.StateMachine.ChangeState(
                new AI.States.PetInvestigateState(robot, targetTerminal)
            );
        }
        else
        {
            Debug.LogWarning("[Lazer Geçidi]: Sahnede RobotBrain bulunamadı!");
        }
    }
}
