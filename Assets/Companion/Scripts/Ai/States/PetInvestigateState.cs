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

            // 1. Þalter doðrudan verildiyse anýnda kilitlen
            if (explicitTerminal != null)
            {
                LockOnToTargetTerminal(explicitTerminal);
                return;
            }

            // Door komutunda yalnýzca bu kapýya baðlý terminali hedefle
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
            // 1. Keþif sýrasýnda görülen kullanýlmamýþ terminale öncelik ver.
            if (currentPhase == Phase.ExploringFrontier && perception != null)
            {
                DoorTerminal visibleTerminal = null;

                // Önce özellikle hedeflenen kapýnýn terminalini ara.
                if (explicitDoor != null)
                    visibleTerminal = perception.FindVisibleTerminalForDoor(explicitDoor);

                // Eþleþen terminal bulunamazsa, görüþteki diðer terminali kontrol et.
                if (visibleTerminal == null)
                    visibleTerminal = perception.DetectedTerminal;

                if (visibleTerminal != null)
                {
                    Debug.Log(
                        $"[Investigate Debug] Terminal bulundu: {visibleTerminal.name} | " +
                        $"Kullanýlmýþ: {visibleTerminal.IsUsed} | " +
                        $"Baþarýsýz listesinde: {failedTerminals.Contains(visibleTerminal)}"
                    );

                    if (!visibleTerminal.IsUsed)
                    {
                        Debug.Log(
                            $"[Investigate Debug] Terminal yeniden hedefleniyor: " +
                            $"{visibleTerminal.name}"
                        );

                        failedTerminals.Remove(visibleTerminal);
                        LockOnToTargetTerminal(visibleTerminal);
                        return;
                    }
                }
            }

            // 2. Keþif hareketi
            if (currentPhase == Phase.ExploringFrontier)
            {
                if (brain.Agent.pathPending) return;
                if (currentTargetNode == null) return;

                if (!brain.Agent.hasPath)
                {
                    if (Time.time - targetIssuedAt >= 1f)
                        RejectCurrentTarget("no route was created");
                    return;
                }

                if (brain.Agent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    RejectCurrentTarget("route is incomplete");
                    return;
                }

                float arrivalDistance =
                    Mathf.Max(0.6f, brain.Agent.stoppingDistance + 0.15f);

                if (brain.Agent.remainingDistance <= arrivalDistance)
                {
                    CompleteFrontierVisit();
                    MoveToNextFrontier();
                }
            }
            // 3. Þaltere yönelme ve etkileþim
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

            // Robot þaltere 2.2m yaklaþtýysa VEYA NavMesh sýnýrýnýn sonuna geldiyse (remainingDistance <= 0.4m) etkileþimi baþlat
            bool reachedNavMeshLimit = brain.Agent.hasPath && brain.Agent.remainingDistance <= 0.4f;

            if (flatDistance <= 1.1f || reachedNavMeshLimit)
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

            if (perception != null)
                perception.UpdateNodeVisibility();

            currentTargetNode.VisitCount++;

            int unknownAfterScan = CountUnknownNeighbors(currentTargetNode);

            if (unknownAfterScan == 0)
            {
                currentTargetNode.State = NodeState.Checked;
            }
            else
            {
                currentTargetNode.State = NodeState.Visited;
            }
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
                currentTargetNode.VisitCount++;

                Debug.LogWarning(
                    $"[Robot AI]: Frontier geçici olarak atlandý ({reason}): " +
                    $"{currentTargetNode.Position} | Ziyaret cezasý: {currentTargetNode.VisitCount}"
                );
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

                if (IsPathCrossingLaser(brain.transform.position, nextNode.Position))
                {
                    Debug.LogWarning($"[Robot AI]: Lazer nedeniyle hedef þimdilik atlandý: {nextNode.Position}");
                    nextNode.VisitCount++;
                    continue;
                }

                currentTargetNode = nextNode;
                targetIssuedAt = Time.time;
                if (brain.Agent != null && brain.Agent.isOnNavMesh)
                {
                    bool destinationSet = brain.Agent.SetDestination(currentTargetNode.Position);

                    Debug.Log(
                        $"[Frontier Debug] Hedef: {currentTargetNode.Position} | " +
                        $"NavMesh: {brain.Agent.isOnNavMesh} | " +
                        $"SetDestination: {destinationSet} | " +
                        $"PathStatus: {brain.Agent.pathStatus}"
                    );

                    if (destinationSet)
                        return;
                }

                Debug.LogWarning($"[Frontier Debug] Hedefe rota atanamadý; düðüm Checked yapýlmadý: {currentTargetNode.Position}");
                currentTargetNode = null;
            }

            if (perception != null)
                perception.UpdateNodeVisibility();

            ExplorationNode finalCandidate = explorer.EvaluateNextFrontier(brain.transform.position);
            if (finalCandidate != null && !IsPathCrossingLaser(brain.transform.position, finalCandidate.Position) &&
                brain.Agent != null && brain.Agent.isOnNavMesh &&
                brain.Agent.SetDestination(finalCandidate.Position))
            {
                currentTargetNode = finalCandidate;
                targetIssuedAt = Time.time;
                return;
            }

            int unknownCount = 0;
            int unknownWithKnownNeighbor = 0;
            int isolatedUnknownCount = 0;
            int unknownWithAnyNeighbor = 0;
            int unknownWithUnknownNeighbor = 0;

            if (knowledgeMap != null)
            {
                foreach (ExplorationNode node in knowledgeMap.AllNodes)
                {
                    if (node.State != NodeState.Unknown)
                        continue;

                    unknownCount++;

                    if (node.Neighbors.Count > 0)
                        unknownWithAnyNeighbor++;

                    bool hasKnownNeighbor = false;
                    bool hasUnknownNeighbor = false;

                    foreach (ExplorationNode neighbor in node.Neighbors)
                    {
                        if (neighbor.State == NodeState.Seen || neighbor.State == NodeState.Visited)
                        {
                            hasKnownNeighbor = true;
                        }

                        if (neighbor.State == NodeState.Unknown)
                        {
                            hasUnknownNeighbor = true;
                        }
                    }

                    if (hasKnownNeighbor) unknownWithKnownNeighbor++;
                    if (hasUnknownNeighbor) unknownWithUnknownNeighbor++;
                    if (!hasKnownNeighbor) isolatedUnknownCount++;
                }
            }

            Debug.LogWarning(
                $"[Frontier Graph Diagnosis] " +
                $"Unknown: {unknownCount} | " +
                $"Herhangi bir komþusu olan: {unknownWithAnyNeighbor} | " +
                $"Unknown komþusu olan: {unknownWithUnknownNeighbor} | " +
                $"Seen/Visited komþusu olan: {unknownWithKnownNeighbor} | " +
                $"Bilinen komþusu olmayan: {isolatedUnknownCount}"
            );
        }

        private bool IsPathCrossingLaser(Vector3 start, Vector3 target)
        {
            NavMeshPath path = new NavMeshPath();

            if (!NavMesh.CalculatePath(start, target, NavMesh.AllAreas, path))
                return true;

            if (path.status != NavMeshPathStatus.PathComplete)
                return true;

            int laserLayer = LayerMask.GetMask("Laser");

            if (laserLayer == 0)
            {
                Debug.LogWarning("[Robot AI] Laser layer bulunamadý.");
                return false;
            }

            const float checkRadius = 0.15f;
            const float heightOffset = 0.3f;

            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Vector3 p1 = path.corners[i] + Vector3.up * heightOffset;
                Vector3 p2 = path.corners[i + 1] + Vector3.up * heightOffset;

                Vector3 delta = p2 - p1;
                float distance = delta.magnitude;

                if (distance <= 0.001f)
                    continue;

                if (Physics.SphereCast(
                    p1,
                    checkRadius,
                    delta / distance,
                    out RaycastHit hit,
                    distance,
                    laserLayer,
                    QueryTriggerInteraction.Collide))
                {
                    Debug.Log($"[Robot AI] Rota elendi. Lazer: {hit.collider.name}, Konum: {hit.point}");
                    return true;
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
            {
                Debug.LogWarning("[Terminal Debug] Erken çýkýþ: terminal, agent veya NavMesh kontrolü baþarýsýz.");
                return false;
            }

            // Terminalin ön yönünü al (ön yüzey +X / right ise)
            Vector3 preferred = targetTerminal.transform.right;
            preferred.y = 0f;

            if (preferred.sqrMagnitude < 0.001f)
                preferred = -brain.transform.forward;

            preferred.y = 0f;
            preferred.Normalize();

            // Duvara monte edilen þalterlerde arka açýlarý (135, 180 vb.) çýkarýp ön açýlara odaklanýyoruz
            float[] angleOffsets = { 0f, 30f, -30f, 60f, -60f, 90f, -90f };
            NavMeshPath candidatePath = new NavMeshPath();

            while (terminalApproachIndex < angleOffsets.Length)
            {
                float angle = angleOffsets[terminalApproachIndex++];
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * preferred;
                Vector3 candidate = targetTerminal.transform.position + direction * 1.0f;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                {
                    Debug.LogWarning($"[Terminal Debug] NavMesh noktasý bulunamadý: {targetTerminal.name} | Aday: {candidate}");
                    continue;
                }

                if (!NavMesh.CalculatePath(brain.Agent.nextPosition, hit.position, NavMesh.AllAreas, candidatePath) ||
                    candidatePath.status != NavMeshPathStatus.PathComplete)
                {
                    Debug.LogWarning($"[Terminal Debug] Tam rota oluþturulamadý: {targetTerminal.name} | Aday: {hit.position}");
                    continue;
                }

                // ÖNEMLÝ: Þaltere kilitlenildiðinde son yaklaþýmdaki IsPathCrossingLaser kontrolü yapýlmaz.
                // Robot þalteri kapatmak için lazerli bölgenin sýnýrýna girmelidir.

                brain.Agent.ResetPath();

                if (!brain.Agent.SetDestination(hit.position))
                {
                    Debug.LogWarning($"[Terminal Debug] SetDestination baþarýsýz: {targetTerminal.name} | Konum: {hit.position}");
                    continue;
                }

                Debug.Log($"[Terminal Debug] Yaklaþma rotasý verildi: {targetTerminal.name} | Hedef: {hit.position}");
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