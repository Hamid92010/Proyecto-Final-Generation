using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Musica de Escenas:\n0. MainMenu\n1. Gameplay\n2. GameOver\n3. Victory")]
    [SerializeField] private AudioClip[] musicScenes;

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
            musicSource.loop = true;
        }
        //Canal para efectos de sonido
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        musicVolume = musicSource.volume;
        sfxSource.volume = musicVolume;
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

    public void StopMusic()
    {
        musicSource.Stop();
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (musicScenes[scene.buildIndex - 1] != null)
        {
            PlayMusic(musicScenes[scene.buildIndex - 1]);
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
