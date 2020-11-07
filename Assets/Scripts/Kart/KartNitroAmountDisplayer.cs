using System;
using UnityEngine;
using UnityEngine.UI;

public class KartNitroAmountDisplayer : MonoBehaviour
{
    [SerializeField] private Text _label;
    [SerializeField] private KartNitro _nitro;

    private void Start()
    {
        _nitro.OnAmountChanged += HandleAmountChanged;
    }

    private void HandleAmountChanged()
    {
        var value = _nitro.PercentageAmount * 100f;
        var valueFormatted = _nitro.PercentageAmount == 0f ? "0%" : $"{value:##}%";
        _label.text = valueFormatted;
    }
}