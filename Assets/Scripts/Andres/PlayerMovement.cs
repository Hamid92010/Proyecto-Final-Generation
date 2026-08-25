using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections;
using System;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset InputActions;
    private InputAction m_moveAction;
    private InputAction m_jumpAction;
    private InputAction i_interactAction;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float fallForce = 0f;
    [SerializeField] private int numberOfJumpsRemaining = 2;

    [Header("Aceleración horizontal")]
    [Tooltip("Segundos en pasar de parado a moveSpeed. 0 = arranque instantáneo, el comportamiento anterior.")]
    [SerializeField] private float timeToTopSpeed = 0.15f;

    [Tooltip("Segundos en pasar de moveSpeed a parado. Súbelo para que Bravard se deslice al soltar la tecla.")]
    [SerializeField] private float timeToStop = 0.12f;

    [Header("Anticipación del salto")]
    // Segundos entre pulsar y aplicar el impulso. Es el tiempo que la pose de
    // encogimiento tiene para verse con el personaje todavía quieto. Es el ÚNICO
    // mando: el Animator no lleva exit time en las salidas de anticipación, así que
    // lo que se ponga aquí es exactamente lo que se ve. Poner 0 devuelve el salto
    // instantáneo. El clip completo dura 0.533 s, así que no conviene pasar de ahí.
    [Tooltip("Segundos que Bravard se encoge en el suelo antes de despegar. 0 = salto instantáneo. Por encima de 0.533 s (el clip completo) se quedaría congelado en el último frame.")]
    [SerializeField] private float groundJumpAnticipation = 0.12f;

    [Tooltip("Igual para el salto aéreo. Durante esta ventana Bravard queda suspendido en el aire y luego despega. El clip completo dura 0.567 s.")]
    [SerializeField] private float airJumpAnticipation = 0.12f;

    [Header("Adelanto del aterrizaje")]
    // El clip de aterrizaje necesita unos frames para levantar la pose. Si se lanza
    // justo al tocar el piso, esos frames se ven CON Bravard ya parado en el suelo y
    // el aterrizaje parece llegar tarde. Avisar con antelación es la trampa que hace
    // que el golpe del clip caiga en el instante del contacto real.
    [Tooltip("Segundos de adelanto con que se avisa del aterrizaje. La animación de caer al suelo arranca antes de tocar, para que el golpe del clip coincida con el contacto. 0 = avisar justo al tocar, el comportamiento anterior.")]
    [SerializeField] private float landingAnticipation = 0.08f;

    // El suelo está lo bastante cerca como para que Bravard lo toque dentro de
    // landingAnticipation segundos
    [SerializeField] private bool isAboutToLand = false;

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 velocityBeforePause;
    [SerializeField] private bool isGrounded = true;
    [SerializeField] private bool wasGrounded = false;
    [SerializeField] private bool isTouchingObstacle = false;
    [SerializeField] private Vector3 groundCheckSize = new Vector3(0.8f, 0.1f, 0.8f);
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    // Tope de saltos encadenados (suelo + aéreo). Evita el número mágico repetido
    private const int maxJumps = 2;

    private GameManager gameManager;
    [SerializeField] private PlayerCollisions playerCollisions;
    [SerializeField] private bool canPlayerMove = true;
    [SerializeField] private bool isInWinTrigger = false;

    // --- Publicación hacia la capa de animación (patrón Observador) ---------
    // Este componente es la única fuente de verdad del estado físico del jugador.
    // PlayerAnimationBridge se suscribe a estos eventos en lugar de leer el Input
    // o repetir el chequeo de suelo por su cuenta.

    // Se dispara cuando se ejecuta un salto desde el suelo
    public event Action OnGroundJump;
    // Se dispara cuando se ejecuta un salto aéreo (doble salto)
    public event Action OnAirJump;

    // Velocidad horizontal con signo que se aplica realmente, entre -moveSpeed y
    // +moveSpeed. Persigue de forma gradual a la que pide el input en lugar de saltar
    // de golpe: es lo que da el arranque y el frenado suaves.
    private float currentHorizontalSpeed;

    // Posición del paso de física anterior, usada para medir el desplazamiento real
    private Vector3 previousPhysicsPosition;
    // Rapidez horizontal real en unidades/segundo, medida por desplazamiento
    private float realHorizontalSpeed;

    // Hay un salto aceptado esperando a que termine su ventana de anticipación
    private bool isAnticipatingJump;
    // Segundos que le faltan a la ventana en curso
    private float jumpAnticipationTimer;
    // Si el salto pendiente salió del suelo o del aire: cambia la ventana y el evento
    private bool pendingJumpIsFromGround;

    // Rapidez horizontal REAL del personaje. No se puede leer de rb.linearVelocity
    // porque el movimiento se aplica con MovePosition, que reposiciona el Rigidbody
    // sin generar velocidad lineal horizontal: siempre devolvería cero.
    public float HorizontalSpeed => realHorizontalSpeed;

    // Velocidad vertical con signo. Aquí sí es válido leer el Rigidbody porque el
    // salto usa AddForce y la caída la integra la gravedad.
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;

    // Resultado del único chequeo de suelo del proyecto
    public bool IsGrounded => isGrounded;

    // Aviso adelantado del aterrizaje. Se vuelve verdadero unos milisegundos ANTES de
    // que IsGrounded lo haga, para que la animación de aterrizaje llegue a tiempo.
    public bool IsAboutToLand => isAboutToLand;

    // Ventana de anticipación abierta: hay un salto aceptado esperando su impulso.
    // La capa de animación la usa para callar la locomoción durante esos milisegundos y
    // dejar que la pose de encogimiento se vea sola. No se puede deducir del Animator
    // porque hay que empezar a callar en el MISMO frame de la pulsación, cuando la
    // transición de entrada todavía no ha terminado.
    public bool IsAnticipatingJump => isAnticipatingJump;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Inicializamos la referencia de medición para que el primer frame no dé un salto de velocidad
        previousPhysicsPosition = rb.position;

        m_moveAction = InputActions.FindAction("Move");
        m_jumpAction = InputActions.FindAction("Jump");
        i_interactAction = InputActions.FindAction("Interact");
    }

    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
        gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.OnGameOver += StopMovement;
            gameManager.OnGameFinished += StopMovement;
            gameManager.OnGamePaused += PausePlayer;
            gameManager.OnGameResumed += ResumePlayer;
        }

        if(playerCollisions != null)
        {
            playerCollisions.TouchObstacle += TouchObstacle;
            playerCollisions.UnTouchObstacle += UnTouchObstacle;
            playerCollisions.StateTriggerWin += StateTriggerWin;
        }
    }

    private void Start()
    {

    }
    private void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
        if (gameManager != null)
        {
            gameManager.OnGameOver -= StopMovement;
            gameManager.OnGameFinished -= StopMovement;
            gameManager.OnGamePaused -= PausePlayer;
            gameManager.OnGameResumed -= ResumePlayer;
        }

        if (playerCollisions != null)
        {
            playerCollisions.TouchObstacle -= TouchObstacle;
            playerCollisions.UnTouchObstacle -= UnTouchObstacle;
            playerCollisions.StateTriggerWin -= StateTriggerWin;
        }
    }

    private void Update()
    {

        moveInput = m_moveAction.ReadValue<Vector2>();

        // Iniciar el juego al detectar movimiento
        if (moveInput.x != 0 && !gameManager.isGameStarted && !gameManager.gameOver)
        {
            gameManager.StartGame();

        }

        // Comprobar si el jugador está en el suelo
        isGrounded = Physics.CheckBox( groundCheck.position + Vector3.down * groundCheckDistance, groundCheckSize / 2f, Quaternion.identity, groundLayer);

        // Mirar el suelo por adelantado depende del chequeo anterior, así que va justo después
        UpdateLandingPrediction();


        if (isGrounded) 
        {
            //Si el jugador esta en el trigger de victoria, y se encuentra en el suelo y presiona la tecla de interactuar, se termina el juego
            if (isInWinTrigger)
            {
                if (i_interactAction.WasPressedThisFrame())
                {
                    AudioManager.Instance.PlayWinTriggerEffect();
                    gameManager.FinishGame();
                }
            }
        }

        //Mecanica de saltos

        RestoreJumps();

        // Descontamos la ventana en curso antes de aceptar un salto nuevo
        UpdateJumpAnticipation();

        // No aceptamos otro salto mientras haya uno esperando su impulso
        if (m_jumpAction.WasPressedThisFrame() && !isAnticipatingJump && (isGrounded || numberOfJumpsRemaining > 0) && !gameManager.gameOver)
        {
            AudioManager.Instance.PlayJumpEffect();
            RequestJump();
        }

    }

    private void FixedUpdate()
    {
        // Medimos el desplazamiento real ocurrido durante el paso de física anterior.
        // Es la única forma fiable de conocer la rapidez horizontal, porque MovePosition
        // mueve el Rigidbody sin dejar rastro en linearVelocity.
        Vector3 realDisplacement = rb.position - previousPhysicsPosition;
        previousPhysicsPosition = rb.position;
        realHorizontalSpeed = new Vector2(realDisplacement.x, realDisplacement.z).magnitude / Time.fixedDeltaTime;

        // Durante la anticipación del salto aéreo sostenemos a Bravard suspendido.
        // No basta con anular la velocidad al pulsar: la gravedad la volvería a
        // acelerar y a los pocos milisegundos cruzaría VerticalVelocity < -0.1, con
        // lo que el Animator cortaría la pose para pasar a la caída.
        if (isAnticipatingJump && !pendingJumpIsFromGround)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }

        Vector3 movement = new Vector3( moveInput.x, 0f, 0f);

        // El input marca la velocidad a la que queremos LLEGAR, no la que se aplica ya
        float targetSpeed = moveInput.x * moveSpeed;

        // Sin control (pausa, game over, knockback) o bloqueado contra un obstáculo en
        // el aire, el objetivo pasa a ser detenerse
        bool movementBlocked = !canPlayerMove || (isTouchingObstacle && !isGrounded);
        if (movementBlocked)
        {
            targetSpeed = 0f;
        }

        // Frenamos cuando el objetivo es menor en magnitud o va en sentido contrario;
        // aceleramos solo cuando el input empuja hacia donde ya nos movemos. Así el
        // cambio de dirección frena hasta cero y vuelve a acelerar, en vez de invertirse
        // de golpe.
        bool braking = Mathf.Abs(targetSpeed) < Mathf.Abs(currentHorizontalSpeed)
                       || targetSpeed * currentHorizontalSpeed < 0f;
        float rampTime = braking ? timeToStop : timeToTopSpeed;

        // Con el tiempo en cero recuperamos el arranque instantáneo de siempre
        float rate = rampTime > 0f ? moveSpeed / rampTime : float.MaxValue;
        currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        // Mover al jugador solo si no está tocando un obstáculo o si está en el suelo
        if (canPlayerMove && (!isTouchingObstacle || isGrounded))
        {
            rb.MovePosition(rb.position + Vector3.right * currentHorizontalSpeed * Time.fixedDeltaTime);
        }

        // Aplicar fuerza adicional de caída si el jugador no está en el suelo
        if (!isGrounded && canPlayerMove)
        {
            rb.AddForce(Vector3.down * fallForce, ForceMode.Acceleration);
        }
        // Rotación
        if (movement != Vector3.zero && canPlayerMove)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);

            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }


    }

    // Fase 1: acepta el salto, consume el contador y abre la ventana de anticipación.
    // El impulso NO se aplica todavía: la animación necesita ese margen para verse con
    // el personaje aún quieto, que es justo lo que antes no ocurría.
    private void RequestJump()
    {
        // Distinguimos el tipo de salto ANTES de consumir el contador, porque de ello
        // depende qué animación (JumpTrigger o DoubleJumpTrigger) debe reproducirse
        pendingJumpIsFromGround = isGrounded;

        numberOfJumpsRemaining--;

        jumpAnticipationTimer = pendingJumpIsFromGround ? groundJumpAnticipation : airJumpAnticipation;
        isAnticipatingJump = true;

        // La animación arranca en este mismo frame: para eso existe la ventana.
        // Solo notificamos cuando el salto fue aceptado de verdad, así que la
        // animación nunca se dispara con un salto rechazado por falta de saltos.
        if (pendingJumpIsFromGround)
        {
            OnGroundJump?.Invoke();
        }
        else
        {
            OnAirJump?.Invoke();
        }

        // Con la ventana en cero recuperamos el salto instantáneo, sin esperar un frame
        if (jumpAnticipationTimer <= 0f)
        {
            ApplyJumpImpulse();
        }
    }

    // Fase 2: descuenta la ventana y lanza el impulso al terminar
    private void UpdateJumpAnticipation()
    {
        // Sin salto pendiente no hay nada que hacer
        if (!isAnticipatingJump)
        {
            return;
        }

        jumpAnticipationTimer -= Time.deltaTime;

        // La pose ya tuvo su momento: toca despegar
        if (jumpAnticipationTimer <= 0f)
        {
            ApplyJumpImpulse();
        }
    }

    // Fase 3: aplica el impulso real
    private void ApplyJumpImpulse()
    {
        isAnticipatingJump = false;

        // Partimos siempre de velocidad vertical cero para que el salto alcance la
        // misma altura venga de donde venga. Es lo que hace fiable al doble salto:
        // cayendo a -7, un impulso de +7 sin anular dejaría al personaje casi parado.
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    // Mira hacia abajo la distancia que Bravard va a recorrer durante la ventana de
    // adelanto. La caja se ESTIRA desde los pies hasta ese punto en vez de desplazarse,
    // para que no se pueda colar una plataforma intermedia sin detectar.
    private void UpdateLandingPrediction()
    {
        // Tocando el suelo el aviso ya no puede llegar tarde
        if (isGrounded)
        {
            isAboutToLand = true;
            return;
        }

        // Subiendo o sin adelanto configurado, el aviso es el contacto real
        float fallSpeed = -rb.linearVelocity.y;
        if (fallSpeed <= 0f || landingAnticipation <= 0f)
        {
            isAboutToLand = false;
            return;
        }

        // Cuanto más rápido cae, más lejos mira: así el aviso siempre llega con los
        // mismos segundos de antelación, independientemente de la velocidad
        float lookAheadDistance = GetLandingLookAheadDistance(fallSpeed);

        isAboutToLand = Physics.CheckBox(
            groundCheck.position + Vector3.down * (lookAheadDistance * 0.5f),
            new Vector3(groundCheckSize.x, lookAheadDistance, groundCheckSize.z) / 2f,
            Quaternion.identity,
            groundLayer);
    }

    // Longitud de la caja de adelanto: lo que ya vigila el chequeo de suelo más lo que
    // Bravard caerá durante la ventana. Compartida con el gizmo para que lo dibujado
    // sea exactamente lo que se consulta.
    private float GetLandingLookAheadDistance(float fallSpeed)
    {
        return groundCheckDistance + fallSpeed * landingAnticipation;
    }


    public void TouchObstacle()
    {
        // Si el jugador no está en el suelo y está tocando un obstáculo y se detiene su movimiento horizontal
        if (!isGrounded)
        {
            isTouchingObstacle = true;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    public void UnTouchObstacle()
    {
        isTouchingObstacle = false;
    }

    private void StopMovement()
    {
        canPlayerMove = false;
    }

    private void ResumeMovement()
    {
        if (gameManager.isGamePaused || gameManager.gameOver || gameManager.gameFinished)
            return;
        canPlayerMove = true;
    }

    private void PausePlayer()
    {
        StopMovement();

        velocityBeforePause = rb.linearVelocity;

        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
    }

    private void ResumePlayer()
    {
        ResumeMovement();

        rb.useGravity = true;
        rb.linearVelocity = velocityBeforePause;
    }

    private void StateTriggerWin(bool value)
    {
        isInWinTrigger = value;
    }

    private void RestoreJumps()
    {
        // Restaurar los saltos cuando el jugador aterriza en el suelo
        if (isGrounded && !wasGrounded)
        {
            numberOfJumpsRemaining = maxJumps;
        }

        wasGrounded = isGrounded;
    }

    public IEnumerator KnockbackCooldown(float duration)
    {

        StopMovement();

        float elapsedTime = 0f;
        float initialHorizontalVelocity = rb.linearVelocity.x;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / duration;

            float currentHorizontalVelocity = Mathf.Lerp(initialHorizontalVelocity, 0f, t);

            rb.linearVelocity = new Vector3( currentHorizontalVelocity, rb.linearVelocity.y, rb.linearVelocity.z);

            yield return null;
        }

        //Quitar fuerza horizontal del knockback, pero mantener la velocidad vertical
        rb.linearVelocity = new Vector3( 0f, rb.linearVelocity.y, rb.linearVelocity.z);
        ResumeMovement();
    }
    private void OnDrawGizmos()
    {
        if (groundCheck == null)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheck.position + Vector3.down * groundCheckDistance, groundCheckSize);

        // Caja de adelanto del aterrizaje, en amarillo. Solo en Play porque depende de
        // la velocidad de caída: se estira al acelerar y debe tocar el suelo un instante
        // antes que las patas. Sirve para afinar landingAnticipation mirando la escena.
        if (!Application.isPlaying || rb == null)
            return;

        float fallSpeed = -rb.linearVelocity.y;
        if (fallSpeed <= 0f || landingAnticipation <= 0f)
            return;

        float lookAheadDistance = GetLandingLookAheadDistance(fallSpeed);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            groundCheck.position + Vector3.down * (lookAheadDistance * 0.5f),
            new Vector3(groundCheckSize.x, lookAheadDistance, groundCheckSize.z));
    }
}
