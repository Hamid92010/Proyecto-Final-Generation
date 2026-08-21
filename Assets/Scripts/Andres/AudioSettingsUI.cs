using UnityEngine;
using UnityEngine.UI;
public class AudioSettingsUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button[] buttonsOnScreen;
    void Start()
    {
        if (musicVolumeSlider != null) 
        {
            musicVolumeSlider.value = AudioManager.Instance.musicVolume;
            musicVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        }
        if(sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = AudioManager.Instance.sfxVolume;
            sfxVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        }
        
        if(buttonsOnScreen != null)
        {
            foreach (Button button in buttonsOnScreen)
            {
                button.onClick.AddListener(() =>
                {
                    AudioManager.Instance.PlayButtonEffect();
                });
            }
        }
        
    }

}
