using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{

    [field: SerializeField]
    public float timeToFinishGame { get; private set; } = 90f;
    [field: SerializeField]
    public float yPosToFinishGame { get; private set; }

    [SerializeField] private GameObject platformToWin;
    public bool isGameStarted = false;
    public bool gameOver = false;
    public bool gameFinished = false;
    public bool isGamePaused = false;


    public event Action OnGameStarted;
    public event Action OnGameOver;
    public event Action OnGameFinished;
    public event Action OnGamePaused;
    public event Action OnGameResumed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        // Si ya existe una instancia y no somos nosotros, destruir este duplicado

    }

    void Start()
    {
        
    }

    public void SearchPlatformToWin()
    {
        platformToWin = GameObject.FindGameObjectWithTag("WinTrigger");
        if (platformToWin != null)
        {
            yPosToFinishGame = platformToWin.transform.position.y;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!isGameStarted)
        {
            return;
        }


        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        { 
            if(isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

    }


    public void StartGame()
    {
        gameFinished = false;
        gameOver = false;
        isGameStarted = true;
        OnGameStarted?.Invoke();
    }

    public void TriggerGameOver()
    {
        gameOver = true;
        isGameStarted = false;
        OnGameOver?.Invoke();
    }

    public void FinishGame()
    {
        gameFinished = true;
        isGameStarted = false;
        OnGameFinished?.Invoke();
    }

    public void PauseGame()
    {
        isGamePaused = true;
        OnGamePaused?.Invoke();
    }

    public void ResumeGame()
    {
        isGamePaused = false;
        OnGameResumed?.Invoke();
    }
}
