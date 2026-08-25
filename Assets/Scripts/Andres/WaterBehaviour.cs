using Unity.VisualScripting;
using UnityEngine;

public class WaterBehaviour : MonoBehaviour
{
    private GameManager gameManager;
    private TimeManager timeManager;
    [SerializeField] private float waterBaseSpeed = 0.1f;
    [SerializeField] private float speedIncreaseRate = 2f;
    [SerializeField] private float speedIncreaseAmount = 0.05f;
    [SerializeField] private float initialYPos = -4f;
    [SerializeField] private float currentWaterSpeed;

    public bool canMoveWater = false;

    private void Awake()
    {
        transform.position = new Vector3(transform.position.x, initialYPos, transform.position.z);
    }
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

            // El agua depende del tiempo transcurrido, asi que parar el temporizador ya
            // la detendria de rebote. Se suscribe igual para no depender de eso: si
            // manana el agua deja de leer el reloj, aqui no habria que acordarse.
            gameManager.OnCutsceneStarted += StopWater;
            gameManager.OnCutsceneEnded += StartWater;
        }

        if (timeManager != null)
        {
            timeManager.OnGameTimeChanged += UpdateWater;
        }
    }

    void Start()
    {


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

        float intervalsPassed = currentTime / speedIncreaseRate;

        currentWaterSpeed = waterBaseSpeed + intervalsPassed * speedIncreaseAmount;

        float newY = initialYPos + currentWaterSpeed * currentTime;

        transform.position = new Vector3( transform.position.x, newY, transform.position.z);
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
            gameManager.OnCutsceneStarted -= StopWater;
            gameManager.OnCutsceneEnded -= StartWater;
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
