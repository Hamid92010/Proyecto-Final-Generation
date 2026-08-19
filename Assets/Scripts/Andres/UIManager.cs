using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private TimeManager timeManager;
    private GameManager gameManager;

    [Header("Referencia del Texto/Panel de Pausa")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Referencia del Texto/Panel del timer")]
    public TextMeshProUGUI timerText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnEnable()
    {
        timeManager = FindAnyObjectByType<TimeManager>();
        gameManager = FindAnyObjectByType<GameManager>();
        if (timeManager != null)
        {
            timeManager.OnTimeChanged += UpdateTimerText;
        }

        if(gameManager != null)
        {
            gameManager.OnGamePaused += ShowPauseMenuPanel;
            gameManager.OnGameResumed += HidePauseMenuPanel;
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeChanged -= UpdateTimerText;
        }
        if(gameManager != null)
        {
            gameManager.OnGamePaused -= ShowPauseMenuPanel;
            gameManager.OnGameResumed -= HidePauseMenuPanel;
        }
    }
    public void UpdateTimerText(float timeToFinishGame)
    {
        int minutes = Mathf.FloorToInt(timeToFinishGame / 60);
        int seconds = Mathf.FloorToInt(timeToFinishGame % 60);
        timerText.text = $"{minutes:0}:{seconds:00}";
    }

    public void ShowPauseMenuPanel()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
    }

    public void HidePauseMenuPanel()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }
}
