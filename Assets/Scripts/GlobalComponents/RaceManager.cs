using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    [SerializeField] private KartRespawnable[] _respawnables;
    private int _currentKartsAlive;

    private void Start()
    {
        _currentKartsAlive = _respawnables.Length;
        foreach (var respawnable in _respawnables)
        {
            respawnable.OnDeath += HandleKartDeath;
        }
    }

    private void OnDestroy()
    {
        foreach (var respawnable in _respawnables)
        {
            respawnable.OnDeath -= HandleKartDeath;
        }
    }

    private void HandleKartDeath()
    {
        _currentKartsAlive--;
        if (_currentKartsAlive == 0)
        {
            HandleAllKartsDeath();
        }
    }

    private void HandleAllKartsDeath()
    {
        Debug.Log("All karts died, respawning");
        _currentKartsAlive = _respawnables.Length;
        foreach (var respawnable in _respawnables)
        {
            respawnable.Respawn();
        }
    }
}
