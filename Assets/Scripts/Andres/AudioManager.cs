using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Musica de Escenas:\n0. Logo\n1. MainMenu\n2. Gameplay\n3. GameOver\n4. Victory")]
    [SerializeField] private AudioClip[] musicScenes;

    [Header("Lista de efectos de sonido")]
    [SerializeField] private AudioClip sfxButtonEffect;
    [SerializeField] private AudioClip sfxPlayerJump;
    [SerializeField] private AudioClip sfxPlayerActivateWinTrigger;
    [SerializeField] private AudioClip sfxImpactBall;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    public float musicVolume;
    public float sfxVolume;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if ( Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        CreateAudioSources();
    }

    public void CreateAudioSources()
    {
        //Canal para musica de fondo
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        //Canal para efectos de sonido
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        musicVolume = musicSource.volume;
        sfxVolume = sfxSource.volume;
    }
    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
        musicVolume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        sfxVolume = volume;
    }
    public void PlayMusic(AudioClip musicClip)
    {
        musicSource.clip = musicClip;
        musicSource.Play();
    }

    public void PlayEffect(AudioClip effectClip)
    {
        sfxSource.PlayOneShot(effectClip);
    }

    public void PlayButtonEffect()
    {
        if (sfxButtonEffect != null)
        {
            PlayEffect(sfxButtonEffect);
        }       
    }

    public void PlayJumpEffect()
    {
        if (sfxPlayerJump != null)
        {
            PlayEffect(sfxPlayerJump);
        }
    }

    public void PlayWinTriggerEffect()
    {
        if (sfxPlayerActivateWinTrigger != null)
        {
            PlayEffect(sfxPlayerActivateWinTrigger);
        }
    }

    public void PlayImpactBallEffect()
    {
        if (sfxImpactBall != null)
        {
            PlayEffect(sfxImpactBall);
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (musicScenes[scene.buildIndex] != null)
        {
            if(scene.buildIndex == 0)
            {
                musicSource.loop = false;
            }
            else
            {
                musicSource.loop = true;
            }
            PlayMusic(musicScenes[scene.buildIndex]);
        }
        else
        {
            StopMusic();
        }
        
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }





}
