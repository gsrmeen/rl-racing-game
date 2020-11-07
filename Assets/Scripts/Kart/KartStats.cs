using UnityEngine;

namespace KartGame.KartSystems
{
    [System.Serializable]
    public struct KartStats
    {
        public float TopSpeed; // "The maximum speed forwards"
        public float Acceleration; // How quickly the Kart reaches top speed.
        public float ReverseSpeed; // The maximum speed backward.
        public float ReverseAcceleration; // The rate at which the kart increases its backward speed.
        public float Braking; // How quickly the Kart slows down when going in the opposite direction.
        public float CoastingDrag; // How quickly to slow down when neither acceleration or reverse is held.
        public float Steer; // How quickly the Kart can turn left and right.
        public float AddedGravity; // Additional gravity for when the Kart is in the air.
        [Range(0.2f, 1)] public float AccelerationCurve; // A higher number means it accelerates faster sooner.
        [Range(0, 1)] public float Grip; // The amount of side-to-side friction.
        [Range(0, 1)] public float Suspension; // How much the Kart tries to keep going forward when on bumpy terrain.
        public float RaycastDist;
        public float MinHeightThreshold;

        public static KartStats GetDefaults()
        {
            return new KartStats
            {
                TopSpeed = 10f,
                Acceleration = 5f,
                AccelerationCurve = 4f,
                Braking = 10f,
                ReverseAcceleration = 5f,
                ReverseSpeed = 5f,
                Steer = 5f,
                CoastingDrag = 4f,
                Grip = .95f,
                AddedGravity = 1f,
                Suspension = .2f,
                RaycastDist = 0.3f,
                MinHeightThreshold = 0.02f
            };
        }
    }
}