using UnityEngine;
using System.Collections;
using System;
using TMPro;
public class TimeManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float currentTimeOfGame;
    [SerializeField] private float timeToFinishGame; // 1:30 minutos
    public TextMeshProUGUI timerText;
    public bool timerStarted = false;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private bool timerCanRun = false;

    public event Action<float> OnTimeChanged;// Tiempo restante para terminar el juego
    public event Action<float> OnGameTimeChanged; //Tiempo transcurrido desde el inicio del juego
    private void Awake()
    {
        
    }

    private void OnEnable()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStarted += StartTimer;
            gameManager.OnGameOver += StopTimer;
            gameManager.OnGameFinished += StopTimer;
            gameManager.OnGamePaused += StopTimer;
            gameManager.OnGameResumed += StartTimer;
            gameManager.OnCutsceneStarted += StopTimer;
            gameManager.OnCutsceneEnded += StartTimer;
        }
    }

    void Start()
    {
        currentTimeOfGame = 0f;
        timeToFinishGame = gameManager.timeToFinishGame;
        NotifyTimeChanged();
    }

    // Update is called once per frame
    void Update()
    {
        if (!timerCanRun)
        {
            return;
        }


        timeToFinishGame -= Time.deltaTime;

        // Se agoto el tiempo. Hasta ahora el contador se quedaba clavado en cero sin
        // avisar a nadie, asi que perder por tiempo simplemente no ocurria.
        if (timeToFinishGame <= 0f)
        {
            timeToFinishGame = 0f;

            // Dejamos el marcador en cero antes de avisar, para que la UI no se quede
            // con el ultimo valor fraccionario mientras corre la animacion de derrota
            NotifyTimeChanged();

            if (gameManager != null)
            {
                gameManager.TriggerGameOver(GameOverCause.Time);
            }

            return;
        }

        currentTimeOfGame += Time.deltaTime;
        NotifyTimeChanged();
        NotifyGameTimeChanged();
        //gameManager.IncreasePainPerSecond(Time.deltaTime);
    }

    private void NotifyTimeChanged()
    {
        OnTimeChanged?.Invoke(timeToFinishGame);
    }
    private void NotifyGameTimeChanged()
    {
        OnGameTimeChanged?.Invoke(currentTimeOfGame);
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStarted -= StartTimer;
            gameManager.OnGameOver -= StopTimer;
            gameManager.OnGameFinished -= StopTimer;
            gameManager.OnGamePaused -= StopTimer;
            gameManager.OnGameResumed -= StartTimer;
            gameManager.OnCutsceneStarted -= StopTimer;
            gameManager.OnCutsceneEnded -= StartTimer;
        }
    }

    private void StartTimer()
    {
        timerCanRun = true;
    }

    private void StopTimer()
    {
        timerCanRun = false;
    }

}
