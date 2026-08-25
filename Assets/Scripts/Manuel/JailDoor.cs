using System.Collections;
using UnityEngine;

// ---------------------------------------------------------------------------
// Segundo paso de la secuencia de victoria: la jaula.
//
// Escucha a la palanca, abre la puerta y solo entonces habilita su trigger de
// victoria. Cuando el jugador entra en ese trigger, la partida esta ganada.
//
// Que las dos condiciones tengan que cumplirse EN ORDEN no se comprueba con
// ningun bool: el trigger de victoria arranca desactivado y nadie salvo la
// palanca puede encenderlo. Sin palanca no hay trigger, y sin trigger no hay
// victoria posible.
//
// Este componente va en el GameObject de la jaula: el que lleva el Animator y
// el BoxCollider marcado como trigger.
// ---------------------------------------------------------------------------
[RequireComponent(typeof(Animator))]
public class JailDoor : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Collider marcado como trigger que declara la victoria. Si se deja vacio se busca el unico trigger que haya en este objeto o sus hijos.")]
    [SerializeField] private Collider victoryTrigger;

    [Header("Enfoque de camara al abrirse")]
    // La puerta ya no se abre en el mismo instante en que la palanca termina. Entre
    // medias la camara sube a ensenar la jaula: es lo que le dice al jugador "ahora
    // ven hacia aca" sin ningun texto. Los tiempos de abajo son esa escena.
    [Tooltip("Desvio del punto al que viaja la camara, relativo al origen de la jaula. Subelo si la jaula queda baja en el encuadre. La Z NO se usa: la camara mantiene su distancia de reposo para no hacer zoom.")]
    [SerializeField] private Vector3 cameraFocusOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("Segundos que tarda la camara en viajar hasta la jaula.")]
    [SerializeField, Min(0f)] private float cameraTravelDuration = 0.6f;

    [Tooltip("Segundos que la camara espera sobre la jaula ANTES de que se abra. Sumado al viaje es el retraso total entre accionar la palanca y ver abrirse la puerta.")]
    [SerializeField, Min(0f)] private float holdBeforeOpening = 0.9f;

    [Tooltip("Segundos que la camara sigue sobre la jaula DESPUES de abrirse, para que se vea la animacion.")]
    [SerializeField, Min(0f)] private float holdAfterOpening = 0.8f;

    [Tooltip("Segundos que tarda la camara en volver al jugador.")]
    [SerializeField, Min(0f)] private float cameraReturnDuration = 0.6f;

    // Animator de la jaula (AnimatorControlles_Jail)
    private Animator jailAnimator;

    // Quien abre la puerta
    private LeverInteraction leverInteraction;

    // Fuente de verdad del desenlace de la partida
    private GameManager gameManager;

    // La victoria se declara una sola vez. FinishGame() no se protege a si mismo,
    // a diferencia de TriggerGameOver, asi que la guarda tiene que estar aqui.
    private bool hasWon = false;

    // Hash del unico parametro del AnimatorControlles_Jail
    private readonly int hashIsLeverActivated = Animator.StringToHash("IsLeverActivated");

    private void Awake()
    {
        jailAnimator = GetComponent<Animator>();

        // El collider solido de la jaula y el de victoria conviven en el prefab:
        // el de victoria es el unico marcado como trigger, y por eso se distingue asi
        if (victoryTrigger == null)
        {
            foreach (Collider candidate in GetComponentsInChildren<Collider>(true))
            {
                if (candidate.isTrigger)
                {
                    victoryTrigger = candidate;
                    break;
                }
            }
        }

        if (victoryTrigger == null)
        {
            Debug.LogError("CRITICO: la jaula no encuentra su collider de victoria (ninguno marcado como trigger). No se podra ganar.", this);
            return;
        }

        // Apagado hasta que la palanca haga su parte. Es lo que impone el orden
        // de las dos condiciones de victoria.
        victoryTrigger.enabled = false;
    }

    private void OnEnable()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        leverInteraction = FindAnyObjectByType<LeverInteraction>();

        if (leverInteraction != null)
        {
            leverInteraction.OnLeverActivated += OpenDoor;
        }
        else
        {
            Debug.LogWarning("La jaula no encuentra ninguna palanca en la escena: no llegara a abrirse.", this);
        }
    }

    private void OnDisable()
    {
        if (leverInteraction != null)
        {
            leverInteraction.OnLeverActivated -= OpenDoor;
        }
    }

    // Abre la puerta y deja lista la segunda condicion de victoria, pero no de golpe:
    // antes se lleva la camara a ensenar la jaula.
    private void OpenDoor()
    {
        StartCoroutine(OpenDoorSequence());
    }

    private IEnumerator OpenDoorSequence()
    {
        CameraFollow cameraFollow = FindAnyObjectByType<CameraFollow>();

        // Congela el mundo entero mientras dura la escena: el jugador, el temporizador,
        // el agua y las rocas. Con la camara en otro sitio no puede defenderse de nada
        // de eso, asi que nada de eso debe seguir avanzando.
        if (gameManager != null)
        {
            gameManager.StartCutscene();
        }

        if (cameraFollow != null)
        {
            cameraFollow.StartFocus(transform, cameraFocusOffset, cameraTravelDuration);

            // El viaje mas la espera son el retraso que se ve antes de que la puerta
            // se mueva: la camara ya esta encima cuando ocurre lo importante
            yield return new WaitForSeconds(cameraTravelDuration + holdBeforeOpening);
        }

        jailAnimator.SetTrigger(hashIsLeverActivated);

        if (victoryTrigger != null)
        {
            victoryTrigger.enabled = true;
        }

        if (cameraFollow != null)
        {
            yield return new WaitForSeconds(holdAfterOpening);

            cameraFollow.EndFocus(cameraReturnDuration);

            yield return new WaitForSeconds(cameraReturnDuration);
        }

        // EndCutscene se guarda a si mismo: si la partida se decidio durante la escena,
        // no reanuda nada aunque se le pida
        if (gameManager != null)
        {
            gameManager.EndCutscene();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryWin(other);
    }

    // Red de seguridad para el caso limite: si el jugador ya estaba dentro del area
    // en el momento de habilitarse el collider, la entrada pudo no llegar a contarse.
    // Estando quieto ahi dentro, Stay si lo detecta.
    private void OnTriggerStay(Collider other)
    {
        TryWin(other);
    }

    // Comprueba si quien toca el trigger gana la partida
    private void TryWin(Collider other)
    {
        // Entrar y salir del trigger varias veces no debe relanzar el desenlace
        if (hasWon)
        {
            return;
        }

        // Solo el jugador gana la partida. Se comprueba por componente y no por el tag
        // "Player" porque es justo lo que se necesita despues: si algun dia hiciera
        // falta actuar sobre el, la referencia ya esta resuelta.
        if (!other.TryGetComponent<PlayerMovement>(out _))
        {
            return;
        }

        hasWon = true;

        if (gameManager != null)
        {
            // A partir de aqui se encadena todo lo demas: se congela al jugador,
            // arrancan las animaciones de victoria de Bravard y de los pollitos, y
            // el UIManager espera victoryLapTime antes de cargar la escena final.
            gameManager.FinishGame();
        }
    }
}
