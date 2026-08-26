using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    private TimeManager timeManager;
    private GameManager gameManager;

    [SerializeField]
    private PlayerCollisions playerCollisions;

    [Header("Panel de pausa")]
    [SerializeField]
    private GameObject pauseMenuPanel;

    [Header("Timer")]
    [SerializeField]
    private TextMeshProUGUI timerText;

    [Header("Panel: presiona E")]
    [SerializeField]
    private GameObject pressEPanel;

    [Header("Escenas")]
    [SerializeField]
    private string gameOverSceneName = "03_GameOver";

    [SerializeField]
    private string victorySceneName = "04_Victory";

    [Header("Tiempos previos a las transiciones")]
    [SerializeField]
    private float gameOverLapTime = 5f;

    [SerializeField]
    private float victoryLapTime = 5f;

    public float GameOverLapTime => gameOverLapTime;

    [Header("Animación de las cortinas")]
    [SerializeField]
    private Animator CourtainAnim;

    [SerializeField]
    private string curtainCloseTrigger = "Close";

    [Tooltip(
        "Duración real de la animación de cierre " +
        "de las cortinas."
    )]
    [SerializeField]
    private float curtainCloseDuration = 10.017f;

    [Header("Elementos que se ocultan al cerrar")]
    [Tooltip(
        "Primer elemento del Canvas que se desactivará " +
        "cuando comiencen a cerrarse las cortinas."
    )]
    [SerializeField]
    private GameObject canvasElementToDisable1;

    [Tooltip(
        "Segundo elemento del Canvas que se desactivará " +
        "cuando comiencen a cerrarse las cortinas."
    )]
    [SerializeField]
    private GameObject canvasElementToDisable2;

    [Header("Fade de la imagen de las cortinas")]
    [Tooltip(
        "CanvasGroup de la imagen que aparece mientras " +
        "las cortinas se cierran."
    )]
    [SerializeField]
    private CanvasGroup curtainImageCanvasGroup;

    [Header("Black Fade")]
    [Tooltip(
        "CanvasGroup de la imagen negra que cubre " +
        "toda la pantalla."
    )]
    [SerializeField]
    private CanvasGroup blackFadeCanvasGroup;

    [Tooltip(
        "Duración del fade de transparente a negro."
    )]
    [SerializeField]
    private float blackFadeDuration = 3f;

    [Tooltip(
        "Tiempo con la pantalla negra antes de " +
        "cargar Victory."
    )]
    [SerializeField]
    private float blackScreenTime = 1f;

    private bool sceneTransitionStarted;

    private void Awake()
    {
        /*
         * La imagen colocada sobre las cortinas
         * comienza transparente.
         */
        PrepareCanvasGroup(
            curtainImageCanvasGroup,
            0f,
            false
        );

        /*
         * Black Fade también comienza transparente.
         */
        PrepareCanvasGroup(
            blackFadeCanvasGroup,
            0f,
            false
        );
    }

    private void OnEnable()
    {
        timeManager =
            FindAnyObjectByType<TimeManager>();

        gameManager =
            FindAnyObjectByType<GameManager>();

        playerCollisions =
            FindAnyObjectByType<PlayerCollisions>();

        if (timeManager != null)
        {
            timeManager.OnTimeChanged +=
                UpdateTimerText;
        }

        if (gameManager != null)
        {
            gameManager.OnGamePaused +=
                ShowPauseMenuPanel;

            gameManager.OnGameResumed +=
                HidePauseMenuPanel;

            gameManager.OnGameOver +=
                LoadGameOverScene;

            gameManager.OnGameFinished +=
                LoadVictoryScene;
        }

        if (playerCollisions != null)
        {
            playerCollisions.StateTriggerWin +=
                TooglePanelEText;
        }
    }

    private void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeChanged -=
                UpdateTimerText;
        }

        if (gameManager != null)
        {
            gameManager.OnGamePaused -=
                ShowPauseMenuPanel;

            gameManager.OnGameResumed -=
                HidePauseMenuPanel;

            gameManager.OnGameOver -=
                LoadGameOverScene;

            gameManager.OnGameFinished -=
                LoadVictoryScene;
        }

        if (playerCollisions != null)
        {
            playerCollisions.StateTriggerWin -=
                TooglePanelEText;
        }
    }

    public void UpdateTimerText(
        float timeToFinishGame
    )
    {
        int minutes = Mathf.FloorToInt(
            timeToFinishGame / 60f
        );

        int seconds = Mathf.FloorToInt(
            timeToFinishGame % 60f
        );

        if (timerText != null)
        {
            timerText.text =
                $"{minutes:0}:{seconds:00}";
        }
    }

    public void TooglePanelEText(bool value)
    {
        if (pressEPanel != null)
        {
            pressEPanel.SetActive(value);
        }
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
        if (sceneTransitionStarted)
        {
            return;
        }

        sceneTransitionStarted = true;

        StartCoroutine(
            LoadGameOverSceneCoroutine()
        );
    }

    public void LoadVictoryScene()
    {
        if (sceneTransitionStarted)
        {
            return;
        }

        sceneTransitionStarted = true;

        StartCoroutine(
            LoadVictorySceneCoroutine()
        );
    }

    private IEnumerator LoadGameOverSceneCoroutine()
    {
        /*
         * Conservar la secuencia original
         * de Game Over.
         */
        yield return new WaitForSecondsRealtime(
            gameOverLapTime
        );

        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            SceneManager.LoadScene(
                gameOverSceneName
            );
        }
        else
        {
            Debug.LogError(
                "No se configuró el nombre de " +
                "la escena Game Over."
            );
        }
    }

    private IEnumerator LoadVictorySceneCoroutine()
    {
        /*
         * 1. Esperar antes de comenzar
         * la transición de victoria.
         */
        yield return new WaitForSecondsRealtime(
            victoryLapTime
        );

        /*
         * 2. Desactivar los dos elementos del Canvas.
         */
        if (canvasElementToDisable1 != null)
        {
            canvasElementToDisable1.SetActive(false);
        }

        if (canvasElementToDisable2 != null)
        {
            canvasElementToDisable2.SetActive(false);
        }

        /*
         * 3. Comenzar el cierre de las cortinas.
         */
        if (CourtainAnim != null)
        {
            CourtainAnim.speed = 1f;

            CourtainAnim.ResetTrigger(
                curtainCloseTrigger
            );

            CourtainAnim.SetTrigger(
                curtainCloseTrigger
            );
        }
        else
        {
            Debug.LogWarning(
                "No se asignó el Animator de " +
                "las cortinas."
            );
        }

        /*
         * 4. Hacer visible la imagen simultáneamente
         * con el cierre de las cortinas.
         */
        if (curtainImageCanvasGroup != null)
        {
            curtainImageCanvasGroup
                .gameObject.SetActive(true);

            curtainImageCanvasGroup.alpha = 0f;

            /*
             * El Animator continúa ejecutándose mientras
             * esta corrutina realiza el fade.
             */
            yield return FadeCanvasGroup(
                curtainImageCanvasGroup,
                0f,
                1f,
                curtainCloseDuration
            );
        }
        else
        {
            /*
             * Esperar de todos modos la duración
             * de la animación de cierre.
             */
            yield return new WaitForSecondsRealtime(
                curtainCloseDuration
            );
        }

        /*
         * 5. Mantener las cortinas completamente cerradas.
         */
        if (CourtainAnim != null)
        {
            CourtainAnim.speed = 0f;
        }

        /*
         * Asegurar que la imagen quede visible.
         */
        if (curtainImageCanvasGroup != null)
        {
            curtainImageCanvasGroup.alpha = 1f;
        }

        /*
         * 6. Después del cierre y del primer fade,
         * comenzar Black Fade.
         */
        if (blackFadeCanvasGroup != null)
        {
            blackFadeCanvasGroup
                .gameObject.SetActive(true);

            blackFadeCanvasGroup.alpha = 0f;

            yield return FadeCanvasGroup(
                blackFadeCanvasGroup,
                0f,
                1f,
                blackFadeDuration
            );

            blackFadeCanvasGroup.alpha = 1f;
        }
        else
        {
            Debug.LogWarning(
                "No se asignó el CanvasGroup " +
                "de Black Fade."
            );
        }

        /*
         * 7. Mantener la pantalla negra.
         */
        yield return new WaitForSecondsRealtime(
            blackScreenTime
        );

        /*
         * 8. Cargar la escena Victory.
         */
        if (!string.IsNullOrEmpty(victorySceneName))
        {
            SceneManager.LoadScene(
                victorySceneName
            );
        }
        else
        {
            Debug.LogError(
                "No se configuró el nombre de " +
                "la escena Victory."
            );
        }
    }

    /// <summary>
    /// Realiza un fade sobre cualquier CanvasGroup.
    /// Usa tiempo real para funcionar aunque
    /// Time.timeScale sea cero.
    /// </summary>
    private IEnumerator FadeCanvasGroup(
        CanvasGroup canvasGroup,
        float startAlpha,
        float targetAlpha,
        float duration
    )
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = startAlpha;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                progress
            );

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// Prepara el estado inicial de un CanvasGroup.
    /// </summary>
    private void PrepareCanvasGroup(
        CanvasGroup canvasGroup,
        float alpha,
        bool blocksRaycasts
    )
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = alpha;
        canvasGroup.interactable = blocksRaycasts;
        canvasGroup.blocksRaycasts = blocksRaycasts;
    }
}