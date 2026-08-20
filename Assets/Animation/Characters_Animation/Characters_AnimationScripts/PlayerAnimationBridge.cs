using UnityEngine;

// ---------------------------------------------------------------------------
// Puente entre la física del jugador y el Animator_Bravard.
//
// Principio rector: el Animator es la máquina de estados. Este script NO decide
// qué animación se reproduce; solo traduce la física a parámetros y deja que el
// Animator resuelva las transiciones con ellos.
//
// Lee de PlayerMovement, que es la única fuente de verdad del proyecto: no toca
// el Rigidbody, no lee el Input y no repite el chequeo de suelo.
//
// Este script vive en el objeto PADRE (el que tiene Rigidbody y PlayerMovement);
// el Animator vive en el objeto HIJO con el modelo del pollito.
// ---------------------------------------------------------------------------
// DefaultExecutionOrder fuerza que este Update() corra DESPUÉS del de PlayerMovement
// (orden 0 por defecto). Sin esto el orden entre ambos es indefinido y podríamos escribir
// en el Animator los valores del frame anterior, con hasta un frame de desfase.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimationBridge : MonoBehaviour
{
    [Header("Referencias Principales")]
    [Tooltip("Arrastra aquí el objeto HIJO (el modelo del pollito) que contiene el Animator")]
    [SerializeField] private Animator animator;

    [Header("Ajuste de Animación")]
    [Tooltip("Suavizado de HorizontalSpeed en segundos. Solo tiene que limar el escalón entre la medición (50 Hz de FixedUpdate) y la lectura (60+ Hz de Update). Debe quedarse pequeño: la rampa de verdad la hace la aceleración de PlayerMovement, y un suavizado alto haría que la animación fuese por detrás del personaje.")]
    [SerializeField] private float horizontalSpeedDamping = 0.05f;

    [Header("Velocidad de la animación de caída")]
    // A qué ritmo se reproduce un clip es una decisión de presentación, no de física:
    // por eso el multiplicador se calcula aquí y no en PlayerMovement.
    [Tooltip("Rapidez de caída (u/s) hasta la que la animación va a velocidad normal.")]
    [SerializeField] private float fallSpeedForNormalPlayback = 4f;

    [Tooltip("Rapidez de caída (u/s) a la que la animación alcanza su velocidad máxima. Un salto normal vuelve al suelo a unos 7 u/s, o sea a mitad de la rampa.")]
    [SerializeField] private float fallSpeedForMaxPlayback = 12f;

    [Tooltip("Multiplicador máximo de la animación de caída. 1 = sin cambio.")]
    [SerializeField] private float maxFallPlaybackSpeed = 2f;

    // Fuente de verdad de la física: velocidad, suelo y saltos
    private PlayerMovement playerMovement;

    // Hashes precalculados de los parámetros del Animator_Bravard
    private readonly int hashHorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
    private readonly int hashVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private readonly int hashIsGrounded = Animator.StringToHash("IsGrounded");
    private readonly int hashIsAboutToLand = Animator.StringToHash("IsAboutToLand");
    private readonly int hashFallPlaybackSpeed = Animator.StringToHash("FallPlaybackSpeed");
    private readonly int hashJumpTrigger = Animator.StringToHash("JumpTrigger");
    private readonly int hashDoubleJumpTrigger = Animator.StringToHash("DoubleJumpTrigger");

    private void Awake()
    {
        // El sistema de movimiento vive en el mismo objeto (lo garantiza RequireComponent)
        playerMovement = GetComponent<PlayerMovement>();

        // Red de seguridad: si se olvidó arrastrar el Animator lo buscamos en los hijos
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Sin Animator el puente no puede trabajar; avisamos y nos apagamos
        if (animator == null)
        {
            Debug.LogError("CRÍTICO: no se encontró ningún Animator en los hijos del jugador. Revisa la jerarquía.", this);
            enabled = false;
            return;
        }

        // El Animator no debe mover al personaje: de eso se encarga el Rigidbody
        animator.applyRootMotion = false;
    }

    private void OnEnable()
    {
        // Un salto es un evento, no un estado deducible de la velocidad: por eso es
        // lo único que llega por suscripción y no por lectura directa de la física
        playerMovement.OnGroundJump += TriggerJump;
        playerMovement.OnAirJump += TriggerDoubleJump;
    }

    private void OnDisable()
    {
        // Cancelamos las suscripciones para no dejar referencias colgando
        playerMovement.OnGroundJump -= TriggerJump;
        playerMovement.OnAirJump -= TriggerDoubleJump;
    }

    private void Update()
    {
        // Rapidez horizontal: alimenta el Blend Tree de Idle / Caminata / Carrera.
        // El suavizado reparte el cambio en el tiempo para que recorra los umbrales.
        animator.SetFloat(hashHorizontalSpeed, playerMovement.HorizontalSpeed, horizontalSpeedDamping, Time.deltaTime);

        // Velocidad vertical SIN suavizar: las condiciones del Animator comparan
        // contra valores exactos (0.2, -0.1) y un valor suavizado las falsearía
        animator.SetFloat(hashVerticalVelocity, playerMovement.VerticalVelocity);

        // Contacto con el suelo, tomado del mismo chequeo que usa el movimiento
        animator.SetBool(hashIsGrounded, playerMovement.IsGrounded);

        // Aviso adelantado de aterrizaje. Es lo que dispara la animación de caer al
        // suelo unos frames ANTES del contacto, para que el golpe del clip y el golpe
        // real coincidan en lugar de dejar un instante de pose congelada.
        animator.SetBool(hashIsAboutToLand, playerMovement.IsAboutToLand);

        // Rapidez de caída: cero mientras sube, positiva mientras baja
        float fallSpeed = Mathf.Max(0f, -playerMovement.VerticalVelocity);

        // InverseLerp ya devuelve un valor recortado entre 0 y 1, así que el multiplicador
        // nunca baja de 1 (la animación no se ralentiza) ni pasa del máximo configurado
        float fallProgress = Mathf.InverseLerp(fallSpeedForNormalPlayback, fallSpeedForMaxPlayback, fallSpeed);
        animator.SetFloat(hashFallPlaybackSpeed, Mathf.Lerp(1f, maxFallPlaybackSpeed, fallProgress));
    }

    // Dispara la animación de salto desde el suelo
    private void TriggerJump()
    {
        // Limpiamos el trigger contrario para que no quede encolado y salte solo después
        animator.ResetTrigger(hashDoubleJumpTrigger);
        animator.SetTrigger(hashJumpTrigger);
    }

    // Dispara la animación de salto aéreo (doble salto)
    private void TriggerDoubleJump()
    {
        // Limpiamos el trigger contrario para que no quede encolado y salte solo después
        animator.ResetTrigger(hashJumpTrigger);
        animator.SetTrigger(hashDoubleJumpTrigger);
    }
}
