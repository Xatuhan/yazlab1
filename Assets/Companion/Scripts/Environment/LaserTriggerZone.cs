
using UnityEngine;
using UnityEngine.InputSystem;

public class LaserTriggerZone : MonoBehaviour
{
    [Header("Bağlı Şalter/Terminal")]
    [SerializeField] private DoorTerminal targetTerminal;

    [Header("Etkileşim Mesafesi")]
    [SerializeField] private float interactionDistance = 5f;
    [SerializeField] private Transform player;

    [Header("Lazer Durumu")]
    [SerializeField] private bool lasersActive = true;

    private bool hasCommandedRobot = false;

    // Diğer script lazerleri açıp kapatırken bunu çağıracak.
    public void SetLasersActive(bool active)
    {
        lasersActive = active;

        // Lazerler yeniden açılırsa etkileşim tekrar kullanılabilsin.
        if (lasersActive)
            hasCommandedRobot = false;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (!lasersActive || hasCommandedRobot || player == null)
            return;

        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        bool isEPressed =
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;

        if (distance <= interactionDistance && isEPressed)
            TriggerRobotToInvestigate();
    }

    private void TriggerRobotToInvestigate()
    {
        if (!lasersActive || targetTerminal == null)
            return;

        RobotBrain robot = FindAnyObjectByType<RobotBrain>();

        if (robot == null)
        {
            Debug.LogWarning(
                "[Lazer Geçidi] Sahnede RobotBrain bulunamadı!",
                this
            );
            return;
        }

        hasCommandedRobot = true;

        robot.StateMachine.ChangeState(
            new AI.States.PetInvestigateState(robot, targetTerminal)
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
