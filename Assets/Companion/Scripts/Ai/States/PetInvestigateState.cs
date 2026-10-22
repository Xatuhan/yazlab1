using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace AI.States
{
    public class PetInvestigateState : IPetState
    {
        private readonly RobotBrain brain;
        private readonly Door targetDoor;
        private DoorTerminal foundTerminal;

        private enum StatePhase { Scanning, Navigating, Interacting }
        private StatePhase currentPhase;

        private readonly float scanRadius = 15f;
        private readonly float interactionMaxDistance = 2.5f; // Duvara gömülü þalterler için esnetilmiþ etkileþim mesafesi
        private Vector3 targetNavMeshPosition; // Robotun duracaðý gerçek NavMesh noktasý

        public PetInvestigateState(RobotBrain brain, Door door)
        {
            this.brain = brain;
            this.targetDoor = door;
        }

        public void Enter()
        {
            if (brain.Agent != null && brain.Agent.isOnNavMesh)
            {
                brain.Agent.isStopped = true;
            }

            brain.StartCoroutine(PerformRadarScan());
        }

        private IEnumerator PerformRadarScan()
        {
            currentPhase = StatePhase.Scanning;
            Debug.Log("<color=cyan>[Robot]: Tarama Modu Aktif! Etraftaki þalter konumlarý taranýyor...</color>");

            int rayCount = 36;
            Vector3 origin = brain.transform.position + Vector3.up * 0.5f;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * (360f / rayCount);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Debug.DrawRay(origin, dir * scanRadius, Color.cyan, 1.5f);
            }

            yield return new WaitForSeconds(1.0f);

            Collider[] hitColliders = Physics.OverlapSphere(brain.transform.position, scanRadius);
            DoorTerminal closestTerminal = null;
            float minDistance = float.MaxValue;

            foreach (var hit in hitColliders)
            {
                DoorTerminal terminal = hit.GetComponent<DoorTerminal>();
                if (terminal != null)
                {
                    float dist = Vector3.Distance(brain.transform.position, terminal.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestTerminal = terminal;
                    }
                }
            }

            if (closestTerminal != null)
            {
                foundTerminal = closestTerminal;

                // Þalter duvarda olduðu için en yakýn geçerli zemin NavMesh noktasýný buluyoruz
                if (NavMesh.SamplePosition(foundTerminal.transform.position, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
                {
                    targetNavMeshPosition = navHit.position;
                }
                else
                {
                    targetNavMeshPosition = foundTerminal.transform.position;
                }

                Debug.Log($"<color=green>[Robot]: Þalter Bulundu -> {foundTerminal.name}! NavMesh üzerindeki güvenli noktaya gidiliyor...</color>");

                currentPhase = StatePhase.Navigating;
                brain.Agent.isStopped = false;
                brain.Agent.SetDestination(targetNavMeshPosition);
            }
            else
            {
                Debug.LogWarning("[Robot]: Tarama alanýnda þalter bulunamadý!");
                brain.StateMachine.ChangeState(new MoveToTargetState(brain));
            }
        }

        public void Update()
        {
            if (currentPhase == StatePhase.Navigating && foundTerminal != null)
            {
                // Robot NavMesh üzerindeki hedef noktasýna vardý mý?
                bool reachedNavMeshPoint = !brain.Agent.pathPending && brain.Agent.remainingDistance <= 0.8f;
                float distanceToTerminal = Vector3.Distance(brain.transform.position, foundTerminal.transform.position);

                if (reachedNavMeshPoint || distanceToTerminal <= interactionMaxDistance)
                {
                    // DUVAR ARKASI KONTROLÜ
                    Vector3 origin = brain.transform.position + Vector3.up * 0.5f;
                    Vector3 targetPos = foundTerminal.transform.position + Vector3.up * 0.5f;

                    if (Physics.Linecast(origin, targetPos, out RaycastHit hit))
                    {
                        if (hit.transform != foundTerminal.transform && !hit.transform.IsChildOf(foundTerminal.transform))
                        {
                            Debug.LogWarning("[Robot]: Þalter baþka bir odada/duvar arkasýnda!");
                            return;
                        }
                    }

                    // Þalter alanýna ulaþýldý, tetikle ve dur
                    currentPhase = StatePhase.Interacting;
                    Debug.Log("<color=green>[Robot]: Þaltere güvenli mesafeden ulaþýldý, buton aktif ediliyor!</color>");

                    brain.Agent.isStopped = true;
                    foundTerminal.ActivateTerminal();
                    brain.StateMachine.ChangeState(new MoveToTargetState(brain));
                }
            }
        }

        public void Exit()
        {
            if (brain.Agent != null && brain.Agent.isOnNavMesh)
            {
                brain.Agent.isStopped = false;
            }
        }
    }
}