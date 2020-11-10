using System;
using UnityEngine;

public class KartAgentRelay : MonoBehaviour
{
    [SerializeField] private Transform[] _raycastSources;

    private float _raycastShotDelay = 0.7f;
    private float _timer;
    private LayerMask _layersToAvoid;
    private RaycastSettings _raycastSettings;

    private void Start()
    {
        _layersToAvoid = Registry.ProjectSettings.globalSettings.layersToAvoid;
        _raycastSettings = Registry.ProjectSettings._raycastSettings;
    }


    private void Update()
    {
        if (_timer >= _raycastSettings.RaycastDelay)
        {
            ShotRaycasts();
            _timer = 0f;
        }

        _timer += Time.deltaTime;
    }

    private void ShotRaycasts()
    {
        foreach (var raycastSource in _raycastSources)
        {
            RaycastHit hit;
            Physics.Raycast(raycastSource.position, raycastSource.forward, out hit, Mathf.Infinity, _layersToAvoid);

            var frac = Mathf.Clamp01((hit.distance - _raycastSettings.VisualMinRange) / _raycastSettings.VisualMaxRange);

            var color = new Color(1f - frac, frac, 0f);

            Debug.DrawRay(raycastSource.position, raycastSource.forward * hit.distance, color,
                _raycastSettings.DebugRaysDuration);
        }
    }
}