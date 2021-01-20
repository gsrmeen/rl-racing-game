using System;
using System.Linq;
using UnityEngine;

public class KartCollisionHandler : MonoBehaviour
{
    public event Action OnRewardCollision;
    public event Action OnLapFinished;

    [SerializeField] private GameObject _preLastCheckPoint;
    [SerializeField] private GameObject _lastCheckPoint;
    [SerializeField] private GameObject _mainCollider;
    private LayerMask _layersToAvoid;
    private LayerMask _rewardLayers;
    private KartRespawnable _respawnable;

    private string _lastRewardTriggerName = "";

    private void Start()
    {
        _layersToAvoid = Registry.ProjectSettings.globalSettings.layersToAvoid;
        _rewardLayers = Registry.ProjectSettings.globalSettings.rewardLayers;
        _respawnable = GetComponent<KartRespawnable>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.contacts.Any(ShouldDieOnCollision))
        {
            HandleDeathCollision();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (CanRewardCollision(other.gameObject))
        {
            if (_lastRewardTriggerName == _preLastCheckPoint.name &&
                other.gameObject.name == _lastCheckPoint.name)
            {
                OnLapFinished?.Invoke();
            }
            
            HandleRewardCollision();
            _lastRewardTriggerName = other.gameObject.name;
        }
    }

    private bool ShouldDieOnCollision(ContactPoint p)
    {
        return p.thisCollider.name == _mainCollider.name &&
               _layersToAvoid.ContainsGameObjectsLayer(p.otherCollider.gameObject);
    }

    private bool CanRewardCollision(GameObject collidedObject)
    {
        return _rewardLayers.ContainsGameObjectsLayer(collidedObject) &&
               collidedObject.name != _lastRewardTriggerName;
    }

    private void HandleDeathCollision()
    {
        gameObject.SetActive(false);
        _respawnable.RaiseOnDeath();
    }

    private void HandleRewardCollision()
    {
        OnRewardCollision?.Invoke();
    }
}