using UnityEngine;
using UnityEngine.InputSystem;

public class LaserTriggerZone : MonoBehaviour
{
    [Header("Bağlı Şalter/Terminal")]
    [Tooltip("Bu lazer alanının kapatması gereken özel şalteri buraya sürükleyin.")]
    [SerializeField] private DoorTerminal targetTerminal;

    private bool isPlayerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("<color=white>[Lazer Geçidi]: Şalteri bulması için robota emret -> [E]</color>");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = false;
    }

    private void Update()
    {
        bool isEPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        if (isPlayerInRange && isEPressed) TriggerRobotToInvestigate();
    }

    private void TriggerRobotToInvestigate()
    {
        RobotBrain robot = FindAnyObjectByType<RobotBrain>();
        if (robot != null)
        {
            Debug.Log("<color=cyan>[Oyuncu]: Robota lazere bağlı özel şalteri açma emri verildi!</color>");
            robot.StateMachine.ChangeState(new AI.States.PetInvestigateState(robot, targetTerminal));
        }
        else Debug.LogWarning("[Lazer Geçidi]: Sahnede RobotBrain bulunamadı!");
    }
}
