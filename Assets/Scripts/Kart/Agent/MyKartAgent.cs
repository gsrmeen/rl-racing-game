using System;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MyKartAgent : Agent, IInput
{
    private Rigidbody _rb;
    private KartAgentRaycaster _raycaster;
    private KartRespawnable _respawnable;
    private Vector2 _movementVector;
    private bool _nitroInput;

    public Vector2 RetrieveMovementInput() => _movementVector;

    public bool RetrieveNitroInput() => _nitroInput;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _raycaster = GetComponent<KartAgentRaycaster>();
        _respawnable = GetComponent<KartRespawnable>();
        _respawnable.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        _respawnable.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        EndEpisode();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(_rb.velocity.magnitude);
        var distances = _raycaster.ShootRaycasts();
        foreach (var distance in distances)
        {
            sensor.AddObservation(distance);
        }
    }

    public override void OnActionReceived(float[] actions)
    {
        ParseActions(actions[0], actions[1], actions[2]);
        
        var velocity = _rb.velocity;
        var velocitySummed = Mathf.Abs(velocity.x) + Mathf.Abs(velocity.z);
        var maxVelocity = 40f;
        var maxReward = 0.25f;
        var reward = maxReward * Mathf.Clamp01(velocitySummed / maxVelocity);
        SetReward(reward);
    }

    private void ParseActions(float x, float z, float nitro)
    {
        _movementVector = Vector2.zero;
        _movementVector.x = Mathf.Clamp(x, -1f, 1f);
        _movementVector.y = Mathf.Clamp(z, -1f, 1f);
        _nitroInput = nitro > 0f;
    }
}