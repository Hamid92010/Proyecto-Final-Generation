using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    [field: SerializeField]
    public float timeToFinishGame { get; private set; } = 90f;
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
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
