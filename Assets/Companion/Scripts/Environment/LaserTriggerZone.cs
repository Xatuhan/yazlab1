
using AI.States;
using UnityEngine;
using UnityEngine.InputSystem;

public class LaserTriggerZone : MonoBehaviour
{
    [Header("Etkileşim Mesafesi")]
    [SerializeField] private float interactionDistance = 5f;
    [SerializeField] private Transform player;

    [Header("Lazer Durumu")]
    [SerializeField] private bool lasersActive = true;

    private bool hasCommandedRobot = false;

    public void SetLasersActive(bool active)
    {
        lasersActive = active;

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
            StartMaze();
    }

    private void StartMaze()
    {
        if (!lasersActive)
            return;

        RobotBrain robot = FindAnyObjectByType<RobotBrain>();

        if (robot == null)
        {
            Debug.LogWarning("[LaserTriggerZone] Robot bulunamadı!");
            return;
        }

        MazeRaycastNavigator mazeNavigator =
            robot.GetComponent<MazeRaycastNavigator>();

        if (mazeNavigator == null)
        {
            Debug.LogWarning(
                "[LaserTriggerZone] MazeRaycastNavigator bulunamadı!"
            );
            return;
        }
        if (robot.StateMachine.CurrentState is PetInvestigateState)
        {
            return;
        }
        if (mazeNavigator.IsActive)
        {
           
            return;
        }

        hasCommandedRobot = true;
        mazeNavigator.StartMazeMode();

       
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            interactionDistance
        );
    }
}
