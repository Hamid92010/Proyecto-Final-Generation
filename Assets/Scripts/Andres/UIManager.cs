using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private TimeManager timeManager;
    private GameManager gameManager;
    [SerializeField] private PlayerCollisions playerCollisions;

    [Header("Referencia del Texto/Panel de Pausa")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Referencia del Texto/Panel del timer")]
    public TextMeshProUGUI timerText;

    [Header("Referencia del Texto/Panel de presiona E para liberar compañero")]
    public GameObject pressEPanel;

    [Header("Referencia de las escenas con su nombre")]
    [SerializeField] private string gameOverSceneName = "01_MainMenu";
    [SerializeField] private string victorySceneName = "01_MainMenu";
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    

    private void OnEnable()
    {
        timeManager = FindAnyObjectByType<TimeManager>();
        gameManager = FindAnyObjectByType<GameManager>();
        playerCollisions = FindAnyObjectByType<PlayerCollisions>();

        if (timeManager != null)
        {
            timeManager.OnTimeChanged += UpdateTimerText;
        }

        if(gameManager != null)
        {
            gameManager.OnGamePaused += ShowPauseMenuPanel;
            gameManager.OnGameResumed += HidePauseMenuPanel;
            gameManager.OnGameOver += LoadGameOverScene;
            gameManager.OnGameFinished += LoadVictoryScene;
        }

        if(playerCollisions != null)
        {
            playerCollisions.StateTriggerWin += TooglePanelEText;
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
            gameManager.OnGameOver -= LoadGameOverScene;
            gameManager.OnGameFinished -= LoadVictoryScene;
        }

        if (playerCollisions != null)
        {
            playerCollisions.StateTriggerWin -= TooglePanelEText;
        }
    }
    public void UpdateTimerText(float timeToFinishGame)
    {
        int minutes = Mathf.FloorToInt(timeToFinishGame / 60);
        int seconds = Mathf.FloorToInt(timeToFinishGame % 60);
        timerText.text = $"{minutes:0}:{seconds:00}";
    }

    public void TooglePanelEText(bool value)
    {
        pressEPanel.SetActive(value);
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

    public void LoadGameOverScene()
    {
        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
        }
    }

    public void LoadVictoryScene()
    {
        if (!string.IsNullOrEmpty(victorySceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(victorySceneName);
        }
    }
}
