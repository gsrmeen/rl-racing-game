using System;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MyKartAgent : Agent, IInput
{
    private Rigidbody _rb;
    private Vector2 _movementVector;
    private bool _nitroInput;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public Vector2 RetrieveMovementInput() => _movementVector;

    public bool RetrieveNitroInput() => _nitroInput;

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(_rb.velocity.magnitude);
        // TODO: add raycast lengths
    }
    
    public override void OnActionReceived(float[] actions)
    {
        ParseActions(actions[0], actions[1], actions[2]);
    }

    private void ParseActions(float x, float z, float nitro)
    {
        _movementVector = Vector2.zero;
        _movementVector.x = x;
        _movementVector.y = z;
        _nitroInput = nitro > 0f;
    }
}