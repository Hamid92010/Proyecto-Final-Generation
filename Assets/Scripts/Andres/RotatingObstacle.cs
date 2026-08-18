using UnityEngine;

public class RotatingObstacle : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }
    
    public bool canObstacleMove = true;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float obstacleSpeedRotation = 30f;
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Z;
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
            RotateObstacle();
        }
    }

    private void RotateObstacle()
    {
        Vector3 rotation = Vector3.zero;

        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotation.x = obstacleSpeedRotation;
                break;

            case RotationAxis.Y:
                rotation.y = obstacleSpeedRotation;
                break;

            case RotationAxis.Z:
                rotation.z = obstacleSpeedRotation;
                break;
        }

        transform.Rotate(rotation * Time.deltaTime);
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
