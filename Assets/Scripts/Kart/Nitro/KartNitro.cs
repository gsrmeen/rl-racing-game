using System;
using KartGame.KartSystems;
using UnityEngine;

public class KartNitro : MonoBehaviour
{
    public event Action OnAmountChanged;
    public float Amount { get; private set; }
    public float PercentageAmount => Amount / _stats.MaxDuration;

    private Rigidbody _rb;
    private bool _active;
    private IInput _input;
    private NitroStats _stats;
    private float _nonActiveTimer;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _input = GetComponent<IInput>();
        _stats = Registry.ProjectSettings.kartStats.NitroStats;
        Amount = _stats.MaxDuration;
    }

    private void Update()
    {
        if (_active == false && _input.RetrieveNitroInput() && IsAvailable())
        {
            ToggleNitro(true);
        }

        if (_active && _input.RetrieveNitroInput() == false)
        {
            ToggleNitro(false);
        }

        if (_active)
        {
            ModifyAmount(-Time.deltaTime);
            _nonActiveTimer = 0f;
            if (IsAvailable() == false)
            {
                ToggleNitro(false);
            }
        }
        else
        {
            _nonActiveTimer += Time.deltaTime;
            if (_nonActiveTimer >= _stats.RechargeDelay)
            {
                ModifyAmount(_stats.MaxDuration);
            }
        }
    }

    private void ModifyAmount(float value)
    {
        Amount = Mathf.Clamp(Amount + value, 0f, _stats.MaxDuration);
        OnAmountChanged?.Invoke();
    }

    private void ToggleNitro(bool toggle)
    {
        _active = toggle;
        if (toggle)
            _rb.velocity *= _stats.VelocityIncrease;
        else
            _rb.velocity /= _stats.VelocityIncrease;
    }

    private bool IsAvailable()
    {
        return Amount > 0f;
    }
}