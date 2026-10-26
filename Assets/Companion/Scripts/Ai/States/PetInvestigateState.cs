using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AI.Core;

namespace AI.States
{
    public class PetInvestigateState : IPetState
    {
        private readonly RobotBrain brain;
        private readonly KnowledgeMap knowledgeMap;
        private readonly PetPerception perception;
        private readonly FrontierExplorer explorer;
        private readonly DoorTerminal explicitTerminal;
        private readonly Door explicitDoor;
        private readonly HashSet<DoorTerminal> failedTerminals = new HashSet<DoorTerminal>();

        private ExplorationNode currentTargetNode;
        private float targetIssuedAt;
        private float terminalPathIssuedAt;
        private float terminalLastProgressAt;
        private float terminalLastRemainingDistance;
        private int terminalApproachIndex;
        private DoorTerminal targetTerminal;

        private enum Phase { ExploringFrontier, LockOnTerminal, Interacting }
        private Phase currentPhase;

        public PetInvestigateState(RobotBrain brain)
        {
            this.brain = brain;
            this.knowledgeMap = brain.GetComponent<KnowledgeMap>();
            this.perception = brain.GetComponent<PetPerception>();
            this.explorer = brain.GetComponent<FrontierExplorer>();
            this.explicitTerminal = null;
            this.explicitDoor = null;
        }

        public PetInvestigateState(RobotBrain brain, Door door) : this(brain)
        {
            this.explicitDoor = door;
        }

        public PetInvestigateState(RobotBrain brain, DoorTerminal explicitTerminal) : this(brain)
        {
            this.explicitTerminal = explicitTerminal;
        }

        public void Enter()
        {
            brain.Agent.isStopped = false;

            if (perception != null)
            {
                perception.ForceInitialScan();
            }

            // 1. Þalter doðrudan verildiyse (ilk kapý/þalter) anýnda kilitlen!
            if (explicitTerminal != null)
            {
                LockOnToTargetTerminal(explicitTerminal);
                return;
            }

            // Door komutunda yalnýzca bu kapýya baðlý terminali hedefle.
            currentPhase = Phase.ExploringFrontier;
            if (explicitDoor != null && perception != null)
            {
                DoorTerminal linkedTerminal = perception.FindVisibleTerminalForDoor(explicitDoor);
                if (linkedTerminal != null)
                {
                    LockOnToTargetTerminal(linkedTerminal);
                    return;
                }
            }
            MoveToNextFrontier();
        }

        public void Update()
        {
            // 1. Keþif sýrasýnda sadece istenen kapýnýn terminaline kilitlen.
            if (currentPhase == Phase.ExploringFrontier && perception != null)
            {
                DoorTerminal visibleTerminal = explicitDoor != null
                    ? perception.FindVisibleTerminalForDoor(explicitDoor)
                    : perception.DetectedTerminal;

                if (visibleTerminal != null && !failedTerminals.Contains(visibleTerminal))
                {
                    LockOnToTargetTerminal(visibleTerminal);
                    return;
                }
            }

            // 2. Keþif Hareketi
            if (currentPhase == Phase.ExploringFrontier)
            {
                if (brain.Agent.pathPending) return;
                if (currentTargetNode == null) return;

                if (!brain.Agent.hasPath)
                {
                    if (Time.time - targetIssuedAt >= 1f) RejectCurrentTarget("no route was created");
                    return;
                }

                if (brain.Agent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    RejectCurrentTarget("route is incomplete");
                    return;
                }

                float arrivalDistance = Mathf.Max(0.6f, brain.Agent.stoppingDistance + 0.15f);
                if (brain.Agent.remainingDistance <= arrivalDistance)
                {
                    CompleteFrontierVisit();
                    MoveToNextFrontier();
                }
            }
            // 3. Þaltere yönelme, eriþilebilir yaklaþým noktalarýný deneme ve etkileþim.
            else if (currentPhase == Phase.LockOnTerminal && targetTerminal != null)
            {
                UpdateTerminalApproach();
            }
        }

        private void UpdateTerminalApproach()
        {
            if (brain.Agent.pathPending)
            {
                if (Time.time - terminalPathIssuedAt >= 3f && !TryNextTerminalApproach())
                    AbandonUnreachableTerminal();
                return;
            }

            Vector3 robotFlat = new Vector3(brain.transform.position.x, 0f, brain.transform.position.z);
            Vector3 terminalFlat = new Vector3(targetTerminal.transform.position.x, 0f, targetTerminal.transform.position.z);
            float flatDistance = Vector3.Distance(robotFlat, terminalFlat);
            if (flatDistance <= 1.5f)
            {
                ActivateTargetTerminal();
                return;
            }

            if (!brain.Agent.hasPath || brain.Agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                if (!TryNextTerminalApproach()) AbandonUnreachableTerminal();
                return;
            }

            float remaining = brain.Agent.remainingDistance;
            if (float.IsInfinity(remaining) || float.IsNaN(remaining))
            {
                if (!TryNextTerminalApproach()) AbandonUnreachableTerminal();
                return;
            }

            if (float.IsInfinity(terminalLastRemainingDistance) || remaining < terminalLastRemainingDistance - 0.1f)
            {
                terminalLastRemainingDistance = remaining;
                terminalLastProgressAt = Time.time;
            }
            else if (Time.time - terminalLastProgressAt >= 3f && !TryNextTerminalApproach())
            {
                AbandonUnreachableTerminal();
            }
        }

        private void ActivateTargetTerminal()
        {
            currentPhase = Phase.Interacting;
            brain.Agent.isStopped = true;
            brain.Agent.ResetPath();

            Vector3 lookDir = (targetTerminal.transform.position - brain.transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                brain.transform.rotation = Quaternion.LookRotation(lookDir);

            Debug.Log($"<color=green>[Robot AI]: Þalter aktif ediliyor: {targetTerminal.name}</color>");
            targetTerminal.ActivateTerminal();
            if (knowledgeMap != null) knowledgeMap.ResetCheckedNodes();
            brain.StateMachine.ChangeState(new MoveToTargetState(brain));
        }

        private void CompleteFrontierVisit()
        {
            if (currentTargetNode == null) return;

            int unknownBeforeScan = CountUnknownNeighbors(currentTargetNode);
            if (perception != null) perception.UpdateNodeVisibility(true);

            currentTargetNode.State = NodeState.Visited;
            currentTargetNode.VisitCount++;
            int unknownAfterScan = CountUnknownNeighbors(currentTargetNode);

            if (unknownAfterScan == 0 || unknownAfterScan >= unknownBeforeScan)
                currentTargetNode.State = NodeState.Checked;
        }

        private int CountUnknownNeighbors(ExplorationNode node)
        {
            int count = 0;
            foreach (ExplorationNode neighbor in node.Neighbors)
                if (neighbor.State == NodeState.Unknown) count++;
            return count;
        }

        private void RejectCurrentTarget(string reason)
        {
            if (currentTargetNode != null)
            {
                currentTargetNode.State = NodeState.Checked;
                Debug.LogWarning($"[Robot AI]: Frontier skipped ({reason}): {currentTargetNode.Position}");
            }

            currentTargetNode = null;
            if (brain.Agent != null && brain.Agent.isOnNavMesh) brain.Agent.ResetPath();
            MoveToNextFrontier();
        }

        private void MoveToNextFrontier()
        {
            if (explorer == null)
            {
                brain.StateMachine.ChangeState(new MoveToTargetState(brain));
                return;
            }

            int attemptsRemaining = knowledgeMap != null ? knowledgeMap.AllNodes.Count : 1;
            while (attemptsRemaining-- > 0)
            {
                ExplorationNode nextNode = explorer.EvaluateNextFrontier(brain.transform.position);
                if (nextNode == null) break;

                // EKSTRA KORUMA: Rota çizgisi üzerinde Lazer var mý kontrol et
                if (IsPathCrossingLaser(brain.transform.position, nextNode.Position))
                {
                    Debug.LogWarning($"[Robot AI]: Lazer tehlikesi içeren rota elendi: {nextNode.Position}");
                    nextNode.State = NodeState.Checked;
                    continue;
                }

                currentTargetNode = nextNode;
                targetIssuedAt = Time.time;
                if (brain.Agent != null && brain.Agent.isOnNavMesh &&
                    brain.Agent.SetDestination(currentTargetNode.Position))
                    return;

                currentTargetNode.State = NodeState.Checked;
                currentTargetNode = null;
            }

            if (perception != null)
                perception.UpdateNodeVisibility(fullCircle: true);

            ExplorationNode finalCandidate = explorer.EvaluateNextFrontier(brain.transform.position);
            if (finalCandidate != null && !IsPathCrossingLaser(brain.transform.position, finalCandidate.Position) &&
                brain.Agent != null && brain.Agent.isOnNavMesh &&
                brain.Agent.SetDestination(finalCandidate.Position))
            {
                currentTargetNode = finalCandidate;
                targetIssuedAt = Time.time;
                return;
            }

            bool unknownRemains = false;
            if (knowledgeMap != null)
            {
                foreach (ExplorationNode node in knowledgeMap.AllNodes)
                {
                    if (node.State == NodeState.Unknown) { unknownRemains = true; break; }
                }
            }
            Debug.LogWarning(unknownRemains
                ? "[Robot AI]: Bilinmeyen alan kaldý ancak bilinen NavMesh üzerinden eriþilebilir frontier yok; takip moduna dönülüyor."
                : "[Robot AI]: Keþfedilebilir frontier kalmadý; takip moduna dönülüyor.");
            brain.StateMachine.ChangeState(new MoveToTargetState(brain));
        }

        /// <summary>
        /// Çizilen NavMesh rotasýnýn lazer katmaný ile çakýþýp çakýþmadýðýný 0.25m küre ile tarar.
        /// Erken tetiklenmeyi önlemek için yarýçap hassas (0.25m) tutulmuþtur.
        /// </summary>
        private bool IsPathCrossingLaser(Vector3 start, Vector3 target)
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(start, target, NavMesh.AllAreas, path))
            {
                int laserLayer = LayerMask.GetMask("Laser");
                if (laserLayer == 0) return false;

                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    Vector3 p1 = path.corners[i] + Vector3.up * 0.3f;
                    Vector3 p2 = path.corners[i + 1] + Vector3.up * 0.3f;
                    Vector3 dir = (p2 - p1).normalized;
                    float dist = Vector3.Distance(p1, p2);

                    // Erken tetiklenme olmasýn diye yarýçap tam agent gövde hassasiyetinde (0.25m)
                    if (Physics.SphereCast(p1, 0.25f, dir, out RaycastHit hit, dist, laserLayer, QueryTriggerInteraction.Collide))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void LockOnToTargetTerminal(DoorTerminal terminal)
        {
            if (terminal == null || terminal.IsUsed) return;

            targetTerminal = terminal;
            currentPhase = Phase.LockOnTerminal;
            terminalApproachIndex = 0;
            brain.Agent.isStopped = false;

            if (!TryNextTerminalApproach())
                AbandonUnreachableTerminal();

            Debug.Log($"<color=green>[Robot AI]: Þaltere eriþilebilir yaklaþým aranýyor: {terminal.name}</color>");
        }

        private bool TryNextTerminalApproach()
        {
            if (targetTerminal == null || brain.Agent == null || !brain.Agent.isOnNavMesh)
                return false;

            Vector3 preferred = brain.transform.position - targetTerminal.transform.position;
            preferred.y = 0f;
            if (preferred.sqrMagnitude < 0.001f) preferred = -brain.transform.forward;
            preferred.y = 0f;
            preferred.Normalize();

            float[] angleOffsets = { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f };
            NavMeshPath candidatePath = new NavMeshPath();
            while (terminalApproachIndex < angleOffsets.Length)
            {
                float angle = angleOffsets[terminalApproachIndex++];
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * preferred;
                Vector3 candidate = targetTerminal.transform.position + direction * 1.25f;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                    continue;

                if (!NavMesh.CalculatePath(brain.Agent.nextPosition, hit.position, NavMesh.AllAreas, candidatePath) ||
                    candidatePath.status != NavMeshPathStatus.PathComplete)
                    continue;

                // Þaltere yaklaþýrken de rota üzerinde lazer var mý denetle
                if (IsPathCrossingLaser(brain.Agent.nextPosition, hit.position))
                    continue;

                brain.Agent.ResetPath();
                if (!brain.Agent.SetDestination(hit.position))
                    continue;

                terminalPathIssuedAt = Time.time;
                terminalLastProgressAt = Time.time;
                terminalLastRemainingDistance = float.PositiveInfinity;
                return true;
            }

            return false;
        }

        private void AbandonUnreachableTerminal()
        {
            if (targetTerminal != null)
            {
                failedTerminals.Add(targetTerminal);
                Debug.LogWarning($"[Robot AI]: Terminal için eriþilebilir ve tamamlanabilir NavMesh rotasý bulunamadý: {targetTerminal.name}");
            }

            targetTerminal = null;
            if (perception != null) perception.ClearDetectedTerminal();
            if (brain.Agent != null && brain.Agent.isOnNavMesh) brain.Agent.ResetPath();
            currentPhase = Phase.ExploringFrontier;
            MoveToNextFrontier();
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