using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//Hola Mundo
/// <summary>
/// Centraliza los trackers de gameplay. Colócalo en el mismo GameObject
/// persistente que SDKAnalytics (o en cualquier objeto que exista mientras
/// dure la partida).
///
/// Se engancha solo a los eventos que ya publican GameManager y
/// PlayerMovement (OnGameStarted, OnGameOver, OnGameFinished, OnGamePaused,
/// OnGameResumed, OnGroundJump, OnAirJump), así que no hace falta llamar a
/// sus métodos desde ningún otro script. Las únicas dos excepciones son
/// GameRestarted() y GameQuitEarly(), que se disparan desde botones de UI
/// (SceneController.LoadGameplayScene / LoadMainMenuScene) porque esas
/// acciones no tienen un evento propio en el que engancharse.
///
/// IMPORTANTE: antes de usar esto en Unity, crea los schemas en el
/// Event Manager del Dashboard con estos nombres exactos:
/// level_completed, game_over, game_restarted, game_quit_early,
/// pause_used, first_time_user, game_completion_time, average_session_length,
/// jumps_performed
/// </summary>
public class GameplayAnalytics : MonoBehaviour
{
    public static GameplayAnalytics Instance { get; private set; }

    private const string FirstTimeUserKey = "analytics_has_launched_before";

    // --- Claves persistentes para "tiempo hasta completar el juego" ---
    // Acumulan tiempo jugado ENTRE SESIONES (incluye reintentos y partidas
    // perdidas) hasta que el jugador gana por primera vez en ese dispositivo.
    private const string HasCompletedGameKey = "analytics_has_completed_game";
    private const string FirstWinAccumSecondsKey = "analytics_first_win_accum_seconds";
    private const string FirstWinAttemptsKey = "analytics_first_win_attempts";

    // --- Claves persistentes para el promedio de duración de sesión ---
    private const string SessionCountKey = "analytics_session_count";
    private const string SessionTotalSecondsKey = "analytics_session_total_seconds";

    // --- Estado interno de la sesión/partida actual ---
    private float levelStartTime;
    private int jumpCount;
    private int groundJumpCount;
    private int airJumpCount;
    private bool pauseInProgress;
    private float pauseStartTime;
    private string currentLevelId;

    // Evita mandar game_quit_early si el nivel ya terminó (game over / victoria)
    private bool levelActive;
    // Solo true justo después de un game_over, para no contar como "restart"
    // el botón de Play del menú principal ni el "jugar de nuevo" tras ganar
    private bool pendingRestartFromGameOver;

    private GameManager gameManager;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnhookGameManager();
        UnhookPlayerMovement();
    }

    private void Start()
    {
        appSessionStartTime = Time.realtimeSinceStartup;
        HookCurrentScene();
        StartCoroutine(SendFirstTimeUserWhenReady());
    }

    // GameManager y PlayerMovement no son persistentes: se recrean cada vez
    // que se carga 02_GamePlay, así que hay que re-buscarlos y re-engancharse
    // en cada carga de escena.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookCurrentScene();
    }

    private void HookCurrentScene()
    {
        UnhookGameManager();
        UnhookPlayerMovement();

        gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStarted += HandleGameStarted;
            gameManager.OnGameOver += HandleGameOver;
            gameManager.OnGameFinished += HandleGameFinished;
            gameManager.OnGamePaused += PauseStarted;
            gameManager.OnGameResumed += PauseEnded;
        }

        playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.OnGroundJump += RegisterGroundJump;
            playerMovement.OnAirJump += RegisterAirJump;
        }
    }

    private void UnhookGameManager()
    {
        if (gameManager == null) return;

        gameManager.OnGameStarted -= HandleGameStarted;
        gameManager.OnGameOver -= HandleGameOver;
        gameManager.OnGameFinished -= HandleGameFinished;
        gameManager.OnGamePaused -= PauseStarted;
        gameManager.OnGameResumed -= PauseEnded;
        gameManager = null;
    }

    private void UnhookPlayerMovement()
    {
        if (playerMovement == null) return;

        playerMovement.OnGroundJump -= RegisterGroundJump;
        playerMovement.OnAirJump -= RegisterAirJump;
        playerMovement = null;
    }

    // ------------------------------------------------------------------
    // first_time_user — se manda una sola vez, la primera vez que alguien
    // abre el juego en ese dispositivo. Se espera a que el SDK esté listo
    // (UnityServices.InitializeAsync es asíncrono) para no perder el evento.
    // ------------------------------------------------------------------
    private IEnumerator SendFirstTimeUserWhenReady()
    {
        bool hasLaunchedBefore = PlayerPrefs.GetInt(FirstTimeUserKey, 0) == 1;
        if (hasLaunchedBefore) yield break;

        float timeout = Time.unscaledTime + 15f;
        while ((SDKAnalytics.Instance == null || !SDKAnalytics.Instance.IsReady) && Time.unscaledTime < timeout)
        {
            yield return null;
        }

        if (SDKAnalytics.Instance == null || !SDKAnalytics.Instance.IsReady) yield break;

        SDKAnalytics.Instance.TrackEvent("first_time_user");
        PlayerPrefs.SetInt(FirstTimeUserKey, 1);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------
    // Enganchado a GameManager.OnGameStarted. Reinicia el cronómetro y el
    // contador de saltos de la partida que empieza.
    // ------------------------------------------------------------------
    private void HandleGameStarted()
    {
        currentLevelId = SceneManager.GetActiveScene().name;
        levelStartTime = Time.time;
        jumpCount = 0;
        groundJumpCount = 0;
        airJumpCount = 0;
        levelActive = true;

        if (PlayerPrefs.GetInt(HasCompletedGameKey, 0) == 0)
        {
            int attempts = PlayerPrefs.GetInt(FirstWinAttemptsKey, 0) + 1;
            PlayerPrefs.SetInt(FirstWinAttemptsKey, attempts);
            PlayerPrefs.Save();
        }
    }

    // ------------------------------------------------------------------
    // Acumula, entre sesiones, el tiempo jugado en intentos que NO terminaron
    // en victoria (game over o quit early), para poder calcular más tarde
    // cuánto tiempo REAL le costó al jugador completar el juego contando los
    // reintentos. Deja de acumular en cuanto ya se completó una vez.
    // ------------------------------------------------------------------
    private void AccumulatePlayTimeTowardFirstWin(float attemptSeconds)
    {
        if (PlayerPrefs.GetInt(HasCompletedGameKey, 0) == 1) return;

        float accumulated = PlayerPrefs.GetFloat(FirstWinAccumSecondsKey, 0f) + attemptSeconds;
        PlayerPrefs.SetFloat(FirstWinAccumSecondsKey, accumulated);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------
    // game_completion_time — se manda una sola vez, la primera vez que el
    // jugador gana en ese dispositivo. total_time_seconds suma el tiempo de
    // TODOS los intentos (incluidos los game over previos), no solo el de la
    // partida ganadora.
    // ------------------------------------------------------------------
    private void CompleteFirstWinTracking(float winningAttemptSeconds)
    {
        if (PlayerPrefs.GetInt(HasCompletedGameKey, 0) == 1) return;

        float totalSeconds = PlayerPrefs.GetFloat(FirstWinAccumSecondsKey, 0f) + winningAttemptSeconds;
        int attempts = PlayerPrefs.GetInt(FirstWinAttemptsKey, 0);

        PlayerPrefs.SetInt(HasCompletedGameKey, 1);
        PlayerPrefs.Save();

        SDKAnalytics.Instance.TrackEvent("game_completion_time", new Dictionary<string, object>
        {
            { "total_time_seconds", totalSeconds },
            { "attempts", attempts }
        });
    }

    // ------------------------------------------------------------------
    // 3) jumps_performed — no se manda por cada salto individual (saturaría
    //    la cuota). Se acumula aquí, enganchado a PlayerMovement.OnGroundJump
    //    / OnAirJump, y se manda como resumen al terminar cada intento
    //    (ver SendJumpsPerformed).
    // ------------------------------------------------------------------
    private void RegisterGroundJump()
    {
        groundJumpCount++;
        jumpCount++;
    }

    private void RegisterAirJump()
    {
        airJumpCount++;
        jumpCount++;
    }

    private void SendJumpsPerformed()
    {
        SDKAnalytics.Instance.TrackEvent("jumps_performed", new Dictionary<string, object>
        {
            { "level_id", currentLevelId },
            { "jumps_count", jumpCount },
            { "ground_jumps", groundJumpCount },
            { "air_jumps", airJumpCount }
        });
    }

    // ------------------------------------------------------------------
    // 1) level_completed — enganchado a GameManager.OnGameFinished.
    // ------------------------------------------------------------------
    private void HandleGameFinished()
    {
        levelActive = false;
        pendingRestartFromGameOver = false;

        float timeSeconds = Time.time - levelStartTime;

        SDKAnalytics.Instance.TrackEvent("level_completed", new Dictionary<string, object>
        {
            { "level_id", currentLevelId },
            { "time_seconds", timeSeconds },
            { "jumps_count", jumpCount }
        });

        CompleteFirstWinTracking(timeSeconds);
        SendJumpsPerformed();
    }

    // ------------------------------------------------------------------
    // 2) game_over — enganchado a GameManager.OnGameOver (el único trigger
    //    de derrota del juego es tocar el agua).
    // ------------------------------------------------------------------
    private void HandleGameOver()
    {
        levelActive = false;
        pendingRestartFromGameOver = true;

        float timeSurvived = Time.time - levelStartTime;

        SDKAnalytics.Instance.TrackEvent("game_over", new Dictionary<string, object>
        {
            { "cause", "water" },
            { "time_survived", timeSurvived },
            { "level_id", currentLevelId },
            { "jumps_count", jumpCount }
        });

        AccumulatePlayTimeTowardFirstWin(timeSurvived);
        SendJumpsPerformed();
    }

    // ------------------------------------------------------------------
    // 4) game_restarted — llamado desde SceneController.LoadGameplayScene().
    //    Solo se manda si venimos de un game_over real (no cuenta el botón
    //    Play del menú principal ni el "jugar de nuevo" tras ganar).
    // ------------------------------------------------------------------
    public void GameRestarted()
    {
        if (!pendingRestartFromGameOver) return;
        pendingRestartFromGameOver = false;

        SDKAnalytics.Instance.TrackEvent("game_restarted", new Dictionary<string, object>
        {
            { "level_id", currentLevelId }
        });
    }

    // ------------------------------------------------------------------
    // 5) game_quit_early — llamado desde SceneController.LoadMainMenuScene().
    //    Solo se manda si el nivel seguía activo (pausa -> volver al menú),
    //    no cuando se vuelve al menú tras un game over o una victoria.
    // ------------------------------------------------------------------
    public void GameQuitEarly()
    {
        if (!levelActive) return;
        levelActive = false;

        float timePlayed = Time.time - levelStartTime;

        SDKAnalytics.Instance.TrackEvent("game_quit_early", new Dictionary<string, object>
        {
            { "level_id", currentLevelId },
            { "time_played", timePlayed },
            { "jumps_count", jumpCount }
        });

        AccumulatePlayTimeTowardFirstWin(timePlayed);
        SendJumpsPerformed();
    }

    // ------------------------------------------------------------------
    // 7) pause_used — enganchado a GameManager.OnGamePaused / OnGameResumed.
    //    El evento se manda al reanudar, con la duración total de esa pausa.
    // ------------------------------------------------------------------
    private void PauseStarted()
    {
        pauseInProgress = true;
        pauseStartTime = Time.unscaledTime; // unscaled por si el juego se congela con Time.timeScale = 0
    }

    private void PauseEnded()
    {
        if (!pauseInProgress) return;

        float pauseDuration = Time.unscaledTime - pauseStartTime;
        pauseInProgress = false;

        SDKAnalytics.Instance.TrackEvent("pause_used", new Dictionary<string, object>
        {
            { "level_id", currentLevelId },
            { "pause_duration", pauseDuration }
        });
    }

    // ------------------------------------------------------------------
    // 6) average_session_length — complementa el 'gameEnded' automático de
    //    Unity. Guarda en PlayerPrefs la suma y el conteo de sesiones de
    //    este dispositivo para poder mandar un promedio local actualizado
    //    en cada cierre, además de la duración de esta sesión concreta.
    // ------------------------------------------------------------------
    private float appSessionStartTime;

    private void OnApplicationQuit()
    {
        float sessionLength = Time.realtimeSinceStartup - appSessionStartTime;

        // Si se cierra la app a mitad de partida, ese tiempo no debe perderse
        // del cómputo de "tiempo hasta la primera victoria".
        if (levelActive)
        {
            AccumulatePlayTimeTowardFirstWin(Time.time - levelStartTime);
        }

        int sessionCount = PlayerPrefs.GetInt(SessionCountKey, 0) + 1;
        float sessionTotal = PlayerPrefs.GetFloat(SessionTotalSecondsKey, 0f) + sessionLength;
        PlayerPrefs.SetInt(SessionCountKey, sessionCount);
        PlayerPrefs.SetFloat(SessionTotalSecondsKey, sessionTotal);
        PlayerPrefs.Save();

        float averageSessionLength = sessionTotal / sessionCount;

        SDKAnalytics.Instance.TrackEvent("average_session_length", new Dictionary<string, object>
        {
            { "session_seconds", sessionLength },
            { "average_session_seconds", averageSessionLength },
            { "session_count", sessionCount }
        });
    }
}
