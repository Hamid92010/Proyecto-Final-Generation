using UnityEngine;
using System.Collections;
using TMPro;
public class TimeManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float currentTimeOfGame;
    public float timeToFinishGame; // 1:30 minutos
    public TextMeshProUGUI timerText;
    public bool timerStarted = false;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UIManager uiManager;

    private void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        uiManager = FindAnyObjectByType<UIManager>();
    }

    void Start()
    {
        currentTimeOfGame = 0f;
        timeToFinishGame = gameManager.timeToFinishGame;
        UpdateTimerText();
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameManager.isGameStarted)
        {
            return;
        }

        if (gameManager.gameOver || gameManager.gameFinished || gameManager.isGamePaused)
        {
            return;
        }


        timeToFinishGame -= Time.deltaTime;
        if(timeToFinishGame < 0)
        {
            timeToFinishGame = 0;
        }
        currentTimeOfGame += Time.deltaTime;
        UpdateTimerText();
        //gameManager.IncreasePainPerSecond(Time.deltaTime);
    }

    private void UpdateTimerText()
    {
        uiManager.UpdateTimerText(timeToFinishGame);
    }

}
