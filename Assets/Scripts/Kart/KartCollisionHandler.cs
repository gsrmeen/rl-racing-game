using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KartCollisionHandler : MonoBehaviour
{
    [SerializeField] private GameObject _mainCollider;
    private LayerMask _layersToAvoid;
    private KartRespawnable _respawnable;

    private void Start()
    {
        _layersToAvoid = Registry.ProjectSettings.globalSettings.layersToAvoid;
        _respawnable = GetComponent<KartRespawnable>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.contacts.Any(p =>
            p.thisCollider.name == _mainCollider.name &&
            _layersToAvoid.ContainsGameObjectsLayer(p.otherCollider.gameObject)))
        {
            HandleCollision();
        }
    }

    private void HandleCollision()
    {
        gameObject.SetActive(false);
        _respawnable.RaiseOnDeath();
    }
}