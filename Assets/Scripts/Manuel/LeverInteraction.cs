using System;
using System.Collections;
using UnityEngine;

// ---------------------------------------------------------------------------
// Primer paso de la secuencia de victoria: la palanca.
//
// Se acciona con la tecla de interactuar mientras el jugador esta dentro del
// trigger de la palanca. Ese "estar dentro y pulsar" ya lo resuelve
// PlayerMovement, que es quien tiene el Input y el chequeo de suelo: aqui solo
// se escucha su aviso.
//
// Al terminar la animacion avisa con OnLeverActivated, que es lo que abre la
// jaula. La palanca NO declara la victoria: solo habilita la segunda condicion.
// ---------------------------------------------------------------------------
public class LeverInteraction : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Animator de la palanca. Si se deja vacio se busca en los hijos.")]
    [SerializeField] private Animator leverAnimator;

    // Se dispara cuando la animacion de la palanca ha TERMINADO, no cuando empieza:
    // la jaula debe abrirse despues del gesto, no a la vez.
    public event Action OnLeverActivated;

    // La palanca es de un solo uso. Sin esto, insistir con la tecla relanzaria la
    // animacion y volveria a abrir una jaula ya abierta.
    [SerializeField] private bool isActivated = false;

    // Quien avisa de la pulsacion
    private PlayerMovement playerMovement;

    // Hash del unico parametro del AnimatorControlles_Lever
    private readonly int hashIsActivated = Animator.StringToHash("IsActivated");

    // La palanca ya cumplio su parte de la secuencia
    public bool IsActivated => isActivated;

    private void Awake()
    {
        // Red de seguridad: si no se arrastro el Animator lo buscamos en los hijos
        if (leverAnimator == null)
        {
            leverAnimator = GetComponentInChildren<Animator>();
        }

        // Sin Animator no hay palanca que accionar y la partida seria inganable,
        // asi que conviene que se vea en consola en lugar de fallar en silencio
        if (leverAnimator == null)
        {
            Debug.LogError("CRITICO: la palanca no encuentra su Animator. La jaula no podra abrirse.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // Mismo patron de busqueda que usa el resto del proyecto
        playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.OnInteract += Activate;
        }
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnInteract -= Activate;
        }
    }

    // Acciona la palanca. Publico para poder dispararlo tambien desde una prueba
    // o desde un boton de depuracion sin tener que simular la pulsacion.
    public void Activate()
    {
        if (isActivated)
        {
            return;
        }

        isActivated = true;

        StartCoroutine(PlayLeverAndWait());
    }

    // Reproduce el gesto y espera a que acabe antes de avisar
    private IEnumerator PlayLeverAndWait()
    {
        leverAnimator.SetTrigger(hashIsActivated);

        // Un trigger no cambia el estado hasta la siguiente evaluacion del Animator:
        // preguntar ahora devolveria todavia el estado de reposo
        yield return null;

        // Durante la mezcla de entrada la duracion que reporta el Animator es la del
        // estado del que venimos, asi que no sirve. Esperamos a estar dentro del clip.
        while (leverAnimator.IsInTransition(0))
        {
            yield return null;
        }

        // La duracion se lee del propio clip en lugar de escribirla a mano: si se
        // sustituye la animacion por otra mas larga, la espera se ajusta sola.
        float leverClipDuration = leverAnimator.GetCurrentAnimatorStateInfo(0).length;

        yield return new WaitForSeconds(leverClipDuration);

        OnLeverActivated?.Invoke();
    }
}
