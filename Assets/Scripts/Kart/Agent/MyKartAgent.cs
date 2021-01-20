using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MyKartAgent : Agent, IInput
{
    private Rigidbody _rb;
    private KartAgentRaycaster _raycaster;
    private KartRespawnable _respawnable;
    private KartCollisionHandler _handler;
    private KartLapCompletion _lapCompletion;
    private AgentSettings _agentSettings;
    private KartNitro _kartNitro;

    private Vector2 _movementVector;
    private bool _nitroInput;

    public Vector2 RetrieveMovementInput() => _movementVector;
    public bool RetrieveNitroInput() => _nitroInput;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _raycaster = GetComponent<KartAgentRaycaster>();
        _respawnable = GetComponent<KartRespawnable>();
        _handler = GetComponent<KartCollisionHandler>();
        _kartNitro = GetComponent<KartNitro>();
        _lapCompletion = GetComponent<KartLapCompletion>();
        _agentSettings = Registry.ProjectSettings.agentSettings;
        _lapCompletion.OnLapCompleted += HandleLapCompletion;
        _handler.OnRewardCollision += HandleRewardCollision;
        _respawnable.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        _lapCompletion.OnLapCompleted -= HandleLapCompletion;
        _respawnable.OnDeath -= HandleDeath;
        _handler.OnRewardCollision -= HandleRewardCollision;
    }

    private void HandleLapCompletion(float time)
    {
        var recorder = Academy.Instance.StatsRecorder;
        recorder.Add("LapCompletionTime", time);
    }
    
    private void HandleDeath()
    {
        EndEpisode();
    }

    private void HandleRewardCollision()
    {
        if (_agentSettings.rewardCheckpoints)
        {
            SetReward(_agentSettings.checkPointReward);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (_agentSettings.observeVelocity)
        {
            sensor.AddObservation(_rb.velocity.magnitude);
        }

        if (_agentSettings.observeNitro)
        {
            sensor.AddObservation(_kartNitro.PercentageAmount);
        }

        var distances = _raycaster.ShootRaycasts();
        foreach (var distance in distances)
        {
            sensor.AddObservation(distance);
        }
    }

    public override void OnActionReceived(float[] actions)
    {
        ParseActions(actions[0], actions[1], actions[2]);

        if (_agentSettings.rewardForLiving)
        {
            SetReward(_agentSettings.livingReward);
        }
        
        if (_agentSettings.rewardVelocity)
        {
            var velocity = _rb.velocity;
            var velocitySummed = Mathf.Abs(velocity.x) + Mathf.Abs(velocity.z);
            var reward = _agentSettings.velocityMaxReward *
                         Mathf.Clamp01(velocitySummed / _agentSettings.approxMaxVelocity);
            SetReward(reward);
        }
    }

    private void ParseActions(float x, float z, float nitro)
    {
        _movementVector = Vector2.zero;
        _movementVector.x = Mathf.Clamp(x, -1f, 1f);
        _movementVector.y = Mathf.Clamp(z, -1f, 1f);
        _nitroInput = nitro > 0f;
    }
}