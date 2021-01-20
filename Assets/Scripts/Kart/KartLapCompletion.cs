using System;
using UnityEngine;

public class KartLapCompletion : MonoBehaviour
{
    public event Action<float> OnLapCompleted;
    private KartCollisionHandler _handler;
    private KartRespawnable _respawnable;
    private float _currentTime;

    private void Start()
    {
        _handler = GetComponent<KartCollisionHandler>();
        _respawnable = GetComponent<KartRespawnable>();
        _handler.OnLapFinished += HandleLapFinished;
        _respawnable.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        _handler.OnLapFinished -= HandleLapFinished;
        _respawnable.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        _currentTime += Time.deltaTime;
    }

    private void HandleLapFinished()
    {
        OnLapCompleted?.Invoke(_currentTime);
        _currentTime = 0f;
    }

    private void HandleDeath()
    {
        _currentTime = 0f;
    }
}