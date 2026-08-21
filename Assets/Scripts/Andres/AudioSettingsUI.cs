using UnityEngine;
using UnityEngine.UI;
public class AudioSettingsUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    void Start()
    {
        musicVolumeSlider.value = AudioManager.Instance.musicVolume;
        sfxVolumeSlider.value = AudioManager.Instance.sfxVolume;

        musicVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
