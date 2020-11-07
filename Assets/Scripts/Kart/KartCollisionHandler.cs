using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KartCollisionHandler : MonoBehaviour
{
    [SerializeField] private GameObject _mainCollider;

    private void OnCollisionEnter(Collision other)
    {
        if (other.contacts.Any(p => p.thisCollider.name == _mainCollider.name))
        {
            HandleCollision();
        }
    }

    private void HandleCollision()
    {
        gameObject.SetActive(false);
    }
}