using UnityEngine;


namespace AI.States
{
    public class PetDeadState : IPetState
    {
        private readonly RobotBrain brain;

        public PetDeadState(RobotBrain brain)
        {
            this.brain = brain;
        }

        public void Enter()
        {
            // NavMesh hareketini durdur
            if (brain.Agent != null && brain.Agent.isOnNavMesh)
            {
                brain.Agent.isStopped = true;
                brain.Agent.ResetPath();
            }

            Debug.Log("<color=red>Robot Lazere Temas Etti! Tepki Veriliyor...</color>");

            // Ýsteðe baðlý: Burada ölüm animasyonu / görsel efekt baþlatýlabilir
        }

        public void Update()
        {
            // Ýleride buraya bekleme süresi veya respawn mantýðý eklenebilir
        }

        public void Exit()
        {
        }
    }
}