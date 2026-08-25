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

    // Abre la puerta y deja lista la segunda condicion de victoria
    private void OpenDoor()
    {
        jailAnimator.SetTrigger(hashIsLeverActivated);

        if (victoryTrigger != null)
        {
            victoryTrigger.enabled = true;
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
