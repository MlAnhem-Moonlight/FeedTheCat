using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeUI : MonoBehaviour
{
    public Slider musicSlider;

    public Slider sfxSlider;

    public TMP_Text musicPercent;

    public TMP_Text sfxPercent;

    public void UpdateMusic()
    {
        musicPercent.text =
            Mathf.RoundToInt(musicSlider.value) + "%";
    }

    public void UpdateSFX()
    {
        sfxPercent.text =
            Mathf.RoundToInt(sfxSlider.value) + "%";
    }

    void Start()
    {
        AudioManager.Instance.PlayMusic("theme_menu");
        UpdateMusic();
        UpdateSFX();
    }
}
