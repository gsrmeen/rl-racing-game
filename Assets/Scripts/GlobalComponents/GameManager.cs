using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private ProjectSettings _projectSettings;


    private void Awake()
    {
        Registry.ProjectSettings = _projectSettings;
    }
}