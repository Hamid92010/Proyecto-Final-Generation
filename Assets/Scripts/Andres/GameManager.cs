using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{

    [field: SerializeField]
    public float timeToFinishGame { get; private set; } = 90f;
    [field: SerializeField]
    public float yPosToFinishGame { get; private set; }

    [SerializeField] private Transform platformToWin;
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
        if (platformToWin != null)
        {
            yPosToFinishGame = platformToWin.position.y;
        }
    }

    void Start()
    {
        
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
        isGameStarted = true;
        OnGameStarted?.Invoke();
    }

    public void TriggerGameOver()
    {
        gameOver = true;
        OnGameOver?.Invoke();
    }

    public void FinishGame()
    {
        gameFinished = true;
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
