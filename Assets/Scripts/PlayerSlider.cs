using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlider : MonoBehaviour
{
    [SerializeField]
    Slider slider;
    [SerializeField]
    TMP_Text nameTxt;

    int index;
    Action<int, float> onValueChanged;

    private void Awake()
    {
        slider.onValueChanged.AddListener((v) =>
        {
            onValueChanged?.Invoke(index, v);
        });
    }

    public void SetIndex(int index)
    {
        this.index = index;
    }

    public void SetName(string name)
    {
        nameTxt.text = name;
    }

    public void SetValueChangedCallback(Action<int, float> cb)
    {
        onValueChanged = cb;
    }

    public void SetValue(float value)
    {
        if (value == slider.value)
            return;
        slider.value = value;
    }

    public float GetValue()
    {
        return slider.value;
    }

    
}
