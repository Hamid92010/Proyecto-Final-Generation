using UnityEngine;

public class RotatingObstacle : MonoBehaviour
{
    public bool canObstacleMove = true;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float obstacleSpeedRotation = 30f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.OnGameOver += StopMovement;
            gameManager.OnGameFinished += StopMovement;
            gameManager.OnGamePaused += StopMovement;
            gameManager.OnGameResumed += ResumeMovement;
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (canObstacleMove && gameManager.isGameStarted)
        {
            transform.Rotate(0f, 0f, -obstacleSpeedRotation * Time.deltaTime);
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameOver -= StopMovement;
            gameManager.OnGameFinished -= StopMovement;
            gameManager.OnGamePaused -= StopMovement;
            gameManager.OnGameResumed -= ResumeMovement;
        }
    }

    private void StopMovement()
    {
        canObstacleMove = false;
    }

    private void ResumeMovement()
    {
        canObstacleMove = true;
    }
}
