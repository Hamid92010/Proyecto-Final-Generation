using Unity.VisualScripting;
using UnityEngine;

public class WaterBehaviour : MonoBehaviour
{
    private GameManager gameManager;
    private TimeManager timeManager;
    [SerializeField] private float waterSpeed = 0.5f;
    [SerializeField] private float initialYPos = -4f;

    public bool canMoveWater = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        timeManager = FindAnyObjectByType<TimeManager>();

        if (gameManager != null)
        {
            gameManager.OnGameStarted += StartWater;
            gameManager.OnGameOver += StopWater;
            gameManager.OnGameFinished += StopWater;
            gameManager.OnGamePaused += StopWater;
            gameManager.OnGameResumed += StartWater;
        }

        if (timeManager != null)
        {
            timeManager.OnGameTimeChanged += UpdateWater;
        }
    }

    void Start()
    {

        transform.position = new Vector3(transform.position.x, initialYPos, transform.position.z);
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    private void UpdateWater(float currentTime)
    {
        if (!canMoveWater)
        {
            return;
        }

        float newY = initialYPos + waterSpeed * currentTime;

        transform.position = new Vector3(
            transform.position.x,
            newY,
            transform.position.z
        );
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStarted -= StartWater;
            gameManager.OnGameOver -= StopWater;
            gameManager.OnGameFinished -= StopWater;
            gameManager.OnGamePaused -= StopWater;
            gameManager.OnGameResumed -= StartWater;
        }

        if (timeManager != null)
        {
            timeManager.OnGameTimeChanged -= UpdateWater;
        }
    }

    private void StartWater()
    {
        canMoveWater = true;
    }

    private void StopWater()
    {
        canMoveWater = false;
    }


}
