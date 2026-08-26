using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class MainMenuSequenceController : MonoBehaviour
{
    [Header("Entrada al menú")]
    [Tooltip(
        "Duración del fade negro de visible a transparente " +
        "cuando comienza la escena del menú."
    )]
    [SerializeField]
    private float initialBlackFadeDuration = 3f;

    [Header("Start View")]
    [SerializeField]
    private CanvasGroupFader startViewFader;

    [SerializeField]
    private float startViewFadeDuration = 4f;

    [Header("Cortinas")]
    [SerializeField]
    private Animator curtainsAnimator;

    [SerializeField]
    private string openTriggerName = "Open";

    [SerializeField]
    private string closeTriggerName = "Close";

    [Tooltip("Duración real del clip Curtains_Open.")]
    [SerializeField]
    private float curtainOpenDuration = 3f;

    [Tooltip(
        "Duración real del clip Curtains_Close. " +
        "En tu proyecto dura 10.017 segundos."
    )]
    [SerializeField]
    private float curtainCloseDuration = 10.017f;

    [Header("Imagen de las cortinas")]
    [SerializeField]
    private CanvasGroupFader curtainImageFader;

    [Tooltip(
        "Duración del fade de salida de la imagen " +
        "cuando se abren las cortinas."
    )]
    [SerializeField]
    private float curtainImageFadeDuration = 3f;

    [Header("Tutorial")]
    [Tooltip(
        "Canvas que contiene las instrucciones " +
        "y el botón Skip."
    )]
    [SerializeField]
    private GameObject tutorialView;

    [Tooltip("Player utilizado durante el tutorial.")]
    [SerializeField]
    private GameObject tutorialPlayer;

    [Tooltip(
        "Tiempo desde que terminan de abrirse las " +
        "cortinas hasta mostrar Tutorial View."
    )]
    [SerializeField]
    private float tutorialViewDelay = 2f;

    [Tooltip(
        "Tiempo desde que aparece Tutorial View " +
        "hasta activar el Player."
    )]
    [SerializeField]
    private float tutorialPlayerDelay = 1f;

    [Tooltip(
        "Tiempo desde que aparece el Player hasta que " +
        "finaliza automáticamente el tutorial."
    )]
    [SerializeField]
    private float automaticTutorialDuration = 5f;

    [Tooltip(
        "Evento opcional ejecutado cuando aparece " +
        "Tutorial View."
    )]
    [SerializeField]
    private UnityEvent onTutorialStarted;

    [Header("Movimiento del Player del tutorial")]
    [Tooltip(
        "NavMeshAgent del Player. Se utiliza como respaldo " +
        "si TutorialGuideAI no está asignado."
    )]
    [SerializeField]
    private NavMeshAgent tutorialPlayerAgent;

    [Tooltip(
        "TutorialGuideAI que controla los waypoints " +
        "y los saltos del Player."
    )]
    [SerializeField]
    private TutorialGuideAI tutorialRouteController;

    [Header("Transición a Gameplay")]
    [Tooltip(
        "BlackFade utilizado tanto al entrar al menú " +
        "como al pasar a Gameplay."
    )]
    [SerializeField]
    private CanvasGroupFader blackFadeFader;

    [Tooltip(
        "Duración del fade de transparente a negro " +
        "después de cerrar las cortinas."
    )]
    [SerializeField]
    private float blackFadeDuration = 3f;

    [Tooltip(
        "Tiempo que permanece la pantalla completamente " +
        "negra antes de cargar Gameplay."
    )]
    [SerializeField]
    private float blackScreenTime = 1f;

    [SerializeField]
    private SceneController sceneController;

    private bool startSequenceRunning;
    private bool tutorialFinished;

    private Coroutine automaticTutorialCoroutine;

    private void Awake()
    {
        /*
         * El escenario del tutorial permanece activo.
         * Solamente se ocultan Tutorial View y el Player.
         */
        if (tutorialView != null)
        {
            tutorialView.SetActive(false);
        }

        if (tutorialPlayer != null)
        {
            tutorialPlayer.SetActive(false);
        }

        /*
         * Preparar BlackFade completamente visible.
         * De esta manera, Game Title View queda cubierto
         * al comenzar la escena.
         */
        if (blackFadeFader != null)
        {
            blackFadeFader.gameObject.SetActive(true);
            blackFadeFader.SetAlpha(1f);
        }
    }

    private void Start()
    {
        /*
         * Comenzar el fade inicial del menú.
         */
        StartCoroutine(InitialMenuFade());
    }

    private IEnumerator InitialMenuFade()
    {
        if (blackFadeFader == null)
        {
            yield break;
        }

        /*
         * Garantizar que comienza completamente negro.
         */
        blackFadeFader.gameObject.SetActive(true);
        blackFadeFader.SetAlpha(1f);

        /*
         * Pasar de negro visible a transparente.
         *
         * El valor true hace que BlackFade se desactive
         * después de llegar a alfa 0.
         */
        yield return blackFadeFader.FadeTo(
            0f,
            initialBlackFadeDuration,
            true
        );
    }

    /// <summary>
    /// Método conectado al botón Start.
    /// </summary>
    public void StartGameSequence()
    {
        if (startSequenceRunning)
        {
            return;
        }

        startSequenceRunning = true;
        StartCoroutine(StartSequence());
    }

    private IEnumerator StartSequence()
    {
        /*
         * 1. Asegurarse de que el Animator funcione.
         */
        if (curtainsAnimator != null)
        {
            curtainsAnimator.speed = 1f;

            curtainsAnimator.ResetTrigger(
                closeTriggerName
            );

            curtainsAnimator.SetTrigger(
                openTriggerName
            );
        }

        /*
         * 2. Desvanecer Start View al mismo tiempo
         * que se abren las cortinas.
         */
        if (startViewFader != null)
        {
            StartCoroutine(
                startViewFader.FadeTo(
                    0f,
                    startViewFadeDuration,
                    true
                )
            );
        }

        /*
         * 3. Esperar hasta que las cortinas
         * terminen de abrirse.
         */
        yield return new WaitForSecondsRealtime(
            curtainOpenDuration
        );

        /*
         * 4. Comenzar el fade de salida de la imagen
         * colocada sobre las cortinas.
         */
        if (curtainImageFader != null)
        {
            StartCoroutine(
                curtainImageFader.FadeTo(
                    0f,
                    curtainImageFadeDuration,
                    true
                )
            );
        }

        /*
         * 5. Esperar para mostrar Tutorial View.
         */
        yield return new WaitForSecondsRealtime(
            tutorialViewDelay
        );

        /*
         * 6. Mostrar Tutorial View.
         */
        if (tutorialView != null)
        {
            tutorialView.SetActive(true);
        }

        /*
         * 7. Ejecutar eventos opcionales.
         */
        onTutorialStarted?.Invoke();

        /*
         * 8. Iniciar el tutorial automático.
         */
        automaticTutorialCoroutine = StartCoroutine(
            RunAutomaticTutorial()
        );
    }

    private IEnumerator RunAutomaticTutorial()
    {
        /*
         * Esperar después de mostrar Tutorial View.
         */
        yield return new WaitForSecondsRealtime(
            tutorialPlayerDelay
        );

        /*
         * No activar al Player si el usuario
         * ya presionó Skip.
         */
        if (tutorialFinished)
        {
            yield break;
        }

        /*
         * Activar al Player.
         */
        if (tutorialPlayer != null)
        {
            tutorialPlayer.SetActive(true);
        }

        /*
         * Esperar mientras el Player realiza
         * su recorrido.
         */
        yield return new WaitForSecondsRealtime(
            automaticTutorialDuration
        );

        automaticTutorialCoroutine = null;

        /*
         * Finalizar automáticamente el tutorial.
         */
        if (!tutorialFinished)
        {
            FinishTutorial();
        }
    }

    /// <summary>
    /// Método conectado exclusivamente al botón Skip.
    /// </summary>
    public void SkipTutorial()
    {
        if (tutorialFinished)
        {
            return;
        }

        /*
         * Detener al Player antes de comenzar
         * la transición de salida.
         */
        StopTutorialPlayerMovement();

        /*
         * Continuar con el cierre de cortinas.
         */
        FinishTutorial();
    }

    private void StopTutorialPlayerMovement()
    {
        /*
         * TutorialGuideAI detiene:
         *
         * - El recorrido por los waypoints.
         * - El NavMeshAgent.
         * - La ruta actual.
         * - La velocidad.
         * - Cualquier salto en curso.
         */
        if (tutorialRouteController != null)
        {
            tutorialRouteController.StopMovement();
            return;
        }

        /*
         * Respaldo si TutorialGuideAI
         * no está asignado.
         */
        if (
            tutorialPlayerAgent != null &&
            tutorialPlayerAgent.enabled &&
            tutorialPlayerAgent.isOnNavMesh
        )
        {
            tutorialPlayerAgent.isStopped = true;
            tutorialPlayerAgent.ResetPath();
            tutorialPlayerAgent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Final automático o manual del tutorial.
    /// No se conecta directamente a ningún botón.
    /// </summary>
    public void FinishTutorial()
    {
        if (tutorialFinished)
        {
            return;
        }

        tutorialFinished = true;

        /*
         * Detener el temporizador automático.
         */
        if (automaticTutorialCoroutine != null)
        {
            StopCoroutine(
                automaticTutorialCoroutine
            );

            automaticTutorialCoroutine = null;
        }

        StartCoroutine(FinishTutorialSequence());
    }

    private IEnumerator FinishTutorialSequence()
    {
        /*
         * 1. Ocultar Tutorial View.
         */
        if (tutorialView != null)
        {
            tutorialView.SetActive(false);
        }

        /*
         * 2. Comenzar a cerrar las cortinas.
         */
        if (curtainsAnimator != null)
        {
            curtainsAnimator.speed = 1f;

            curtainsAnimator.ResetTrigger(
                openTriggerName
            );

            curtainsAnimator.SetTrigger(
                closeTriggerName
            );
        }

        /*
         * 3. Reactivar la imagen colocada sobre
         * las cortinas con alfa 0.
         */
        if (curtainImageFader != null)
        {
            curtainImageFader.gameObject.SetActive(true);
            curtainImageFader.SetAlpha(0f);

            /*
             * Hacer visible la imagen durante el mismo
             * tiempo que tarda el cierre de las cortinas.
             *
             * No utilizamos yield porque ambas acciones
             * deben ocurrir simultáneamente.
             */
            StartCoroutine(
                curtainImageFader.FadeTo(
                    1f,
                    curtainCloseDuration
                )
            );
        }

        /*
         * 4. Esperar la duración completa
         * de Curtains_Close.
         */
        yield return new WaitForSecondsRealtime(
            curtainCloseDuration
        );

        /*
         * 5. Mantener las cortinas cerradas.
         */
        if (curtainsAnimator != null)
        {
            curtainsAnimator.speed = 0f;
        }

        /*
         * Garantizar que la imagen de las cortinas
         * quede completamente visible.
         */
        if (curtainImageFader != null)
        {
            curtainImageFader.SetAlpha(1f);
        }

        /*
         * 6. Después de que las cortinas terminaron
         * de cerrarse, reactivar BlackFade con alfa 0.
         */
        if (blackFadeFader != null)
        {
            blackFadeFader.gameObject.SetActive(true);
            blackFadeFader.SetAlpha(0f);

            /*
             * Pasar progresivamente de transparente
             * a completamente negro.
             */
            yield return blackFadeFader.FadeTo(
                1f,
                blackFadeDuration
            );
        }
        else
        {
            Debug.LogWarning(
                "No se asignó BlackFadeFader en " +
                "MainMenuSequenceController."
            );
        }

        /*
         * 7. Esperar un segundo después de que
         * BlackFade quedó completamente visible.
         */
        yield return new WaitForSecondsRealtime(
            blackScreenTime
        );

        /*
         * 8. Cargar la escena Gameplay.
         */
        if (sceneController != null)
        {
            sceneController.LoadGameplayScene();
        }
        else
        {
            Debug.LogError(
                "No se asignó SceneController en " +
                "MainMenuSequenceController."
            );
        }
    }
}