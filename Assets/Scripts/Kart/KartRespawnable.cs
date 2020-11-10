using System;
using UnityEngine;

public class KartRespawnable : MonoBehaviour
{
    public event Action OnDeath;
    private Vector3 _respawnPos;
    private Quaternion _respawnRot;

    private void Start()
    {
        _respawnPos = transform.position;
        _respawnRot = transform.rotation;
    }

    public void Respawn()
    {
        transform.SetPositionAndRotation(_respawnPos, _respawnRot);
        gameObject.SetActive(true);
    }

    public void RaiseOnDeath()
    {
        OnDeath?.Invoke();
    }
}