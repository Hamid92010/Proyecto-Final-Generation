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

    [Header("Silenciado de la locomoción")]
    // Hay poses que necesitan el cuerpo entero para leerse: el encogimiento del salto y
    // la recomposición del aterrizaje. Mientras se reproducen, la locomoción deja de
    // reportar velocidad al Animator para no competir por la pose. El personaje NO se
    // detiene: sigue desplazándose con toda su inercia. Solo se calla la animación.
    [Tooltip("Segundos en que la locomoción se calla al abrirse una ventana. Debe quedarse POR DEBAJO de la duración de la transición de entrada a la anticipación (0.075 s): si tarda más, el Blend Tree todavía va camino de Idle cuando la mezcla ya terminó. 0 = corte seco, se vería el salto de pose.")]
    [SerializeField] private float locomotionMuteAttack = 0.05f;

    [Tooltip("Segundos en que la locomoción recupera la voz al cerrarse la ventana. Conviene parecido a la duración de la transición de salida hacia Locomotion (0.2 s), para que el Blend Tree llegue ya en carrera al final de la mezcla en lugar de aterrizar en Idle y acelerar después.")]
    [SerializeField] private float locomotionMuteRelease = 0.18f;

    // Tag con el que se marcan en el Animator los estados que piden el cuerpo entero.
    // Se pone a mano en el campo Tag del Inspector del estado. Hoy solo lo lleva
    // Jump_End (el de JumpSystem), que es donde silenciar surte efecto: su salida
    // temprana depende de HorizontalSpeed.
    private const string bodyOwningStateTag = "Landing";

    // 1 = locomoción a pleno rendimiento, 0 = callada. Multiplica a HorizontalSpeed.
    private float locomotionWeight = 1f;

    // Fuente de verdad de la física: velocidad, suelo y saltos
    private PlayerMovement playerMovement;

    // Quien detecta los golpes. Vive en este mismo objeto, igual que el movimiento.
    private PlayerCollisions playerCollisions;

    // Quien decide el desenlace de la partida. El puente traduce fisica a parametros,
    // pero ganar y perder no son fisica: son estados de juego, y su unica fuente es esta.
    private GameManager gameManager;

    // Hashes precalculados de los parámetros del Animator_Bravard
    private readonly int hashHorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
    private readonly int hashVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private readonly int hashIsGrounded = Animator.StringToHash("IsGrounded");
    private readonly int hashIsAboutToLand = Animator.StringToHash("IsAboutToLand");
    private readonly int hashFallPlaybackSpeed = Animator.StringToHash("FallPlaybackSpeed");
    private readonly int hashJumpTrigger = Animator.StringToHash("JumpTrigger");
    private readonly int hashDoubleJumpTrigger = Animator.StringToHash("DoubleJumpTrigger");

    // Desenlaces de la partida. Pese al prefijo "Is", los cuatro de abajo son TRIGGERS
    // en el Animator; el unico bool de verdad del grupo es IsDead.
    private readonly int hashIsHit = Animator.StringToHash("IsHit");
    private readonly int hashIsDefeatedByWater = Animator.StringToHash("IsDefeatedByWater");
    private readonly int hashIsDefeatedByTime = Animator.StringToHash("IsDefeatedByTime");
    private readonly int hashIsWinning = Animator.StringToHash("IsWinning");
    private readonly int hashIsDead = Animator.StringToHash("IsDead");

    private void Awake()
    {
        // El sistema de movimiento vive en el mismo objeto (lo garantiza RequireComponent)
        playerMovement = GetComponent<PlayerMovement>();

        // Las colisiones tambien viven aqui. No lo garantiza ningun RequireComponent,
        // asi que puede faltar y quien se suscribe lo comprueba antes.
        playerCollisions = GetComponent<PlayerCollisions>();

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

        // El golpe es igual de instantaneo, pero quien lo ve es el detector de
        // colisiones, no el movimiento
        if (playerCollisions != null)
        {
            playerCollisions.HitByBall += TriggerHit;
        }

        // El GameManager se busca aqui y no en Awake por el mismo motivo que en
        // PlayerMovement y TimeManager: es el patron que sigue todo el proyecto
        gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.OnGameOverCaused += TriggerDefeat;
            gameManager.OnGameFinished += TriggerVictory;
        }
    }

    private void OnDisable()
    {
        // Cancelamos las suscripciones para no dejar referencias colgando
        playerMovement.OnGroundJump -= TriggerJump;
        playerMovement.OnAirJump -= TriggerDoubleJump;

        if (playerCollisions != null)
        {
            playerCollisions.HitByBall -= TriggerHit;
        }

        if (gameManager != null)
        {
            gameManager.OnGameOverCaused -= TriggerDefeat;
            gameManager.OnGameFinished -= TriggerVictory;
        }
    }

    private void Update()
    {
        // Peso de la locomoción antes de escribir nada: decide si el Blend Tree tiene voz
        UpdateLocomotionWeight();

        // Rapidez horizontal: alimenta el Blend Tree de Idle / Caminata / Carrera.
        // Multiplicada por el peso, así que durante las ventanas protegidas cae hacia 0 y
        // el Blend Tree se va a Idle aunque Bravard siga desplazándose a toda velocidad.
        float reportedHorizontalSpeed = playerMovement.HorizontalSpeed * locomotionWeight;

        // Con el peso a medio camino el escalón ya lo está suavizando la rampa del peso.
        // Sumar aquí el suavizado de régimen sería el doble suavizado que advierte la
        // documentación: la animación se quedaría por detrás del personaje.
        float damping = locomotionWeight < 1f ? 0f : horizontalSpeedDamping;

        animator.SetFloat(hashHorizontalSpeed, reportedHorizontalSpeed, damping, Time.deltaTime);

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

    // Persigue el peso de la locomoción hacia 0 o 1 según haya o no una ventana abierta
    private void UpdateLocomotionWeight()
    {
        bool muteLocomotion = ShouldMuteLocomotion();

        float targetWeight = muteLocomotion ? 0f : 1f;
        float rampTime = muteLocomotion ? locomotionMuteAttack : locomotionMuteRelease;

        // Con el tiempo en cero el cambio es inmediato, sin rampa
        locomotionWeight = rampTime > 0f
            ? Mathf.MoveTowards(locomotionWeight, targetWeight, Time.deltaTime / rampTime)
            : targetWeight;
    }

    // Si hay una pose que debe verse sin que la locomoción compita por el cuerpo.
    // Son dos ventanas y se detectan de forma DISTINTA a propósito: cada una necesita
    // empezar en un momento que la otra vía no sabría ver.
    private bool ShouldMuteLocomotion()
    {
        // 1) La anticipación del salto. Hay que empezar a callar en el mismo frame de la
        //    pulsación, cuando el Animator está todavía EN la transición de entrada y su
        //    estado actual sigue siendo Locomotion. Por eso no sirve el Tag y se lee la
        //    ventana de la física, que es quien la abre y la cierra.
        if (playerMovement.IsAnticipatingJump)
        {
            return true;
        }

        // 2) Estar en transición devuelve la voz a la locomoción. Es lo que hace que al
        //    arrancar la salida del aterrizaje hacia Locomotion el Blend Tree ya venga
        //    subiendo hacia la carrera, en lugar de llegar a Idle y acelerar después.
        if (animator.IsInTransition(0))
        {
            return false;
        }

        // 3) El aterrizaje. Aquí el Tag sí vale: el estado ya está a peso completo y lo
        //    único que hace falta es que su salida temprana por HorizontalSpeed no salte,
        //    para que la recomposición se reproduzca entera.
        return animator.GetCurrentAnimatorStateInfo(0).IsTag(bodyOwningStateTag);
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

    // Reacción al golpe de una pelota. Es la única de las cuatro que no termina la
    // partida: el Animator vuelve solo a Locomotion al acabar el clip, así que aquí
    // no hay que deshacer nada ni tocar IsDead.
    private void TriggerHit()
    {
        // La reaccion al golpe sale de AnyState, asi que tambien interrumpiria la pose
        // final: una pelota que llegue mientras Bravard se ahoga o celebra lo sacaria
        // de ahi para hacerle el gesto de dolor. Con la partida decidida, se ignora.
        if (gameManager != null && (gameManager.gameOver || gameManager.gameFinished))
        {
            return;
        }

        animator.SetTrigger(hashIsHit);
    }

    // Derrota, con la animación que corresponda al motivo.
    //
    // El orden importa: la transición del Animator exige IsDead ADEMÁS del trigger, y
    // los triggers se consumen en cuanto se evalúan. Si el bool llegase después, la
    // condición se comprobaría con IsDead todavía en false, el trigger se gastaría sin
    // provocar el cambio de estado y Bravard se quedaría corriendo mientras se ahoga.
    private void TriggerDefeat(GameOverCause cause)
    {
        ClearPendingJumpTriggers();

        animator.SetBool(hashIsDead, true);

        if (cause == GameOverCause.Water)
        {
            animator.SetTrigger(hashIsDefeatedByWater);
        }
        else
        {
            animator.SetTrigger(hashIsDefeatedByTime);
        }
    }

    // Victoria. La transición pide justo lo contrario que la derrota: IsDead en false.
    // Ya es su valor por defecto, pero se escribe explícitamente para que ganar no
    // dependa de que nadie lo haya puesto a true antes.
    private void TriggerVictory()
    {
        ClearPendingJumpTriggers();

        animator.SetBool(hashIsDead, false);
        animator.SetTrigger(hashIsWinning);
    }

    // Victoria y derrota son estados terminales, pero AnyState sigue vigilando los
    // triggers de salto: uno encolado justo antes del desenlace sacaría a Bravard de
    // la pose final para hacerle saltar, y de ahí ya no habría vuelta.
    private void ClearPendingJumpTriggers()
    {
        animator.ResetTrigger(hashJumpTrigger);
        animator.ResetTrigger(hashDoubleJumpTrigger);
    }
}
