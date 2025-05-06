using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DemoPlayerUI : MonoBehaviour
{
    [SerializeField]
    PlayerSlider sliderPrefab;
    

    DemoPlayer player;
    List<PlayerSlider> sliders;

    public void InitAndCreate(DemoPlayer demoPlayer)
    {
        player = demoPlayer;
        sliders = new List<PlayerSlider>();
        sliderPrefab.gameObject.SetActive(false);

        int trackCount = demoPlayer.GetTrackCount();
        for (int i = 0; i < trackCount; ++i)
        {
            var trackSlider = GameObject.Instantiate(sliderPrefab, transform);
            trackSlider.gameObject.SetActive(true);
            trackSlider.SetIndex(i);
            trackSlider.SetName(player.GetTrackName(i));
            trackSlider.SetValueChangedCallback(onValueCb);
            sliders.Add(trackSlider);
        }
    }

    private void onValueCb(int sliderIndex,float value)
    {
        player.SetTrackVolume(sliderIndex, value);
    }

    private void Update()
    {
        if (player == null || sliders == null)
            return;

        float[] volumes = player.GetVolumes();
        for (int i = 0; i < volumes.Length; ++i)
        {
            SetSliderValue(i, volumes[i]);
        }
    }

    private void SetSliderValue(int sliderIndex,float value)
    {
        if (sliders == null || sliders.Count <= sliderIndex)
            return;

        var slider = sliders[sliderIndex];
        if (slider.GetValue() == value)
            return;

        slider.SetValue(value);
    }
}
