using System;
using System.Diagnostics;
using KartGame.KartSystems;
using UnityEngine;

public class KartNitro : MonoBehaviour
{
    private Rigidbody _rb;
    private float _speedIncrease = 2.5f;
    private bool _held;
    private KeyCode _nitroKeyCode = KeyCode.T;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKey(_nitroKeyCode) && !_held)
        {
            _held = true;
            SwitchNitro(true);
        }

        if (Input.GetKeyUp(_nitroKeyCode) && _held)
        {
            _held = false;
            SwitchNitro(false);
        }
    }

    private void SwitchNitro(bool b)
    {
        if (b)
        {
            _rb.velocity *= _speedIncrease;
        }
        else
        {
            _rb.velocity /= _speedIncrease;
        }
    }
}