
using System.Collections.Generic;
using UnityEngine;

namespace AI.Core
{
    public class LaserObservation
    {
        public bool IsActive { get; private set; }
        public Vector3 Position { get; private set; }
        public float LastObservedTime { get; private set; }

        public int ObservationCount { get; private set; }

        public List<float> ActivationTimes { get; private set; }
            = new List<float>();

        public List<float> DeactivationTimes { get; private set; }
            = new List<float>();

        public List<Vector3> PositionHistory { get; private set; }
            = new List<Vector3>();

        public LaserObservation(
            bool initialState,
            Vector3 initialPosition)
        {
            IsActive = initialState;
            Position = initialPosition;
            LastObservedTime = Time.time;
            ObservationCount = 0;
        }

        public void Record(
            bool isActive,
            Vector3 position)
        {
            float now = Time.time;

            // Durum deðiþimini kaydet.
            if (isActive != IsActive)
            {
                if (isActive)
                    ActivationTimes.Add(now);
                else
                    DeactivationTimes.Add(now);
            }

            IsActive = isActive;
            Position = position;
            LastObservedTime = now;

            ObservationCount++;

            PositionHistory.Add(position);
        }
    }
}
