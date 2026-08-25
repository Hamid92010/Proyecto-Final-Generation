using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Motivo por el que se perdio la partida. Bravard no se hunde igual que se queda
// sin tiempo, asi que la animacion de derrota depende de cual de los dos fue.
public enum GameOverCause
{
    Water,
    Time
}

public class GameManager : MonoBehaviour
{

    [field: SerializeField]
    public float timeToFinishGame { get; private set; } = 90f;
    [field: SerializeField]
    public float yPosToFinishGame { get; private set; }

    [SerializeField] private GameObject platformToWin;
    public bool isGameStarted = false;
    public bool gameOver = false;
    public bool gameFinished = false;
    public bool isGamePaused = false;

    // Hay una escena guionizada en marcha (hoy, la camara ensenando la jaula al
    // abrirse). El jugador no controla nada durante esos segundos, asi que el mundo
    // tampoco debe seguir corriendo en su contra.
    public bool isCutscenePlaying = false;


    public event Action OnGameStarted;
    public event Action OnGameOver;
    // Mismo aviso que OnGameOver pero indicando la causa. Va aparte para no romper
    // a los cuatro suscriptores que ya existen y a los que la causa les da igual.
    public event Action<GameOverCause> OnGameOverCaused;
    public event Action OnGameFinished;
    public event Action OnGamePaused;
    public event Action OnGameResumed;

    // Congelan el mundo SIN abrir el menu de pausa. Es la diferencia con OnGamePaused:
    // aquella es cosa del jugador y muestra el panel; esta la pide el propio juego y
    // debe pasar desapercibida, porque la escena es lo que se esta mirando.
    public event Action OnCutsceneStarted;
    public event Action OnCutsceneEnded;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        // Si ya existe una instancia y no somos nosotros, destruir este duplicado

    }

    void Start()
    {
        
    }

    public void SearchPlatformToWin()
    {
        platformToWin = GameObject.FindGameObjectWithTag("WinTrigger");
        if (platformToWin != null)
        {
            yPosToFinishGame = platformToWin.transform.position.y;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!isGameStarted)
        {
            return;
        }


        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        { 
            if(isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

    }


    public void StartGame()
    {
        gameFinished = false;
        gameOver = false;
        isGameStarted = true;
        OnGameStarted?.Invoke();
    }

    public void TriggerGameOver(GameOverCause cause)
    {
        // Una partida solo se pierde una vez: sin esto el agua podria reavisar cada
        // frame de contacto y relanzar la animacion de derrota ya empezada
        if (gameOver)
        {
            return;
        }

        gameOver = true;

        // El motivo se avisa primero para que la animacion arranque en el mismo frame
        OnGameOverCaused?.Invoke(cause);
        OnGameOver?.Invoke();
    }

    // Version historica sin motivo. El unico game over que existia era el del agua,
    // asi que es el que se asume para no cambiar el comportamiento de quien ya llamaba.
    public void TriggerGameOver()
    {
        TriggerGameOver(GameOverCause.Water);
    }

    public void FinishGame()
    {
        gameFinished = true;
        OnGameFinished?.Invoke();
    }

    public void StartCutscene()
    {
        // Dos escenas encadenadas no deben avisar dos veces: los suscriptores se
        // congelarian una vez pero se reanudarian a la primera que terminase
        if (isCutscenePlaying)
        {
            return;
        }

        isCutscenePlaying = true;
        OnCutsceneStarted?.Invoke();
    }

    public void EndCutscene()
    {
        if (!isCutscenePlaying)
        {
            return;
        }

        isCutscenePlaying = false;

        // Si la partida se decidio o se pauso mientras corria la escena, el mundo tiene
        // que quedarse parado: reanudar aqui volveria a poner en marcha el tiempo y el
        // agua por encima de un game over. Cuando se despause, OnGameResumed lo hara.
        if (gameOver || gameFinished || isGamePaused)
        {
            return;
        }

        OnCutsceneEnded?.Invoke();
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
