using System;
using UnityEngine;

public class KartAgentRaycaster : MonoBehaviour
{
    [SerializeField] private Transform[] _raycastSources;

    private LayerMask _layersToAvoid;
    private RaycastSettings _raycastSettings;

    private void Start()
    {
        _layersToAvoid = Registry.ProjectSettings.globalSettings.layersToAvoid;
        _raycastSettings = Registry.ProjectSettings._raycastSettings;
    }

    private void Update()
    {
        ShootRaycasts();
    }

    public float[] ShootRaycasts()
    {
        var distances = new float[_raycastSources.Length];
        for (int i = 0; i < _raycastSources.Length; i++)
        {
            var source = _raycastSources[i];
            RaycastHit hit;
            Physics.Raycast(source.position, source.forward, out hit, Mathf.Infinity, _layersToAvoid);

            var frac = Mathf.Clamp01((hit.distance - _raycastSettings.VisualMinRange) /
                                     _raycastSettings.VisualMaxRange);

            var color = new Color(1f - frac, frac, 0f);

            // Debug.DrawRay(source.position, source.forward * hit.distance, color,
            //     _raycastSettings.DebugRaysDuration);

            distances[i] = hit.distance;
        }

        return distances;
    }
}