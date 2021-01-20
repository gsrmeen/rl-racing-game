using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectSettings", menuName = "Settings/ProjectSettings")]
public class ProjectSettings : ScriptableObject
{
    public GlobalSettings globalSettings;
    public AgentSettings agentSettings;
    public KartStats kartStats;
    public RaycastSettings _raycastSettings;
}

[Serializable]
public struct GlobalSettings
{
    public LayerMask layersToAvoid;
    public LayerMask rewardLayers;
}

[Serializable]
public struct AgentSettings
{
    // Observations
    public bool observeVelocity;
    public bool observeNitro;
    
    // Rewards
    public bool rewardVelocity;
    public float velocityMaxReward;
    public float approxMaxVelocity;
    public bool rewardCheckpoints;
    public float checkPointReward;
    public bool rewardForLiving;
    public float livingReward;
}

[Serializable]
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
    public NitroStats NitroStats;
}

[Serializable]
public struct NitroStats
{
    public float VelocityIncrease;
    public float MaxDuration;
    public float RechargeDelay;
}

[Serializable]
public struct RaycastSettings
{
    public float DebugRaysDuration;
    public float VisualMinRange;
    public float VisualMaxRange;
}