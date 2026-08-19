using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections;

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

    private GameManager gameManager;
    [SerializeField] private PlayerCollisions playerCollisions;
    [SerializeField] private bool canPlayerMove = true;
    [SerializeField] private bool isInWinTrigger = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

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
        if (moveInput.x != 0 && !gameManager.isGameStarted)
        {
            gameManager.StartGame();

        }

        // Comprobar si el jugador está en el suelo
        isGrounded = Physics.CheckBox( groundCheck.position + Vector3.down * groundCheckDistance, groundCheckSize / 2f, Quaternion.identity, groundLayer);
        

        if (isGrounded) 
        {
            //Si el jugador esta en el trigger de victoria, y se encuentra en el suelo y presiona la tecla de interactuar, se termina el juego
            if (isInWinTrigger)
            {
                if (i_interactAction.WasPressedThisFrame())
                {
                    gameManager.FinishGame();
                }
            }
        }

        //Mecanica de saltos

        RestoreJumps();
        if (m_jumpAction.WasPressedThisFrame() && (isGrounded || numberOfJumpsRemaining > 0))
        {           
            Jump();           
        }

    }

    private void FixedUpdate()
    {
        Vector3 movement = new Vector3( moveInput.x, 0f, 0f);

        // Mover al jugador solo si no está tocando un obstáculo o si está en el suelo
        if (canPlayerMove && (!isTouchingObstacle || isGrounded))
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
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

    private void Jump()
    {
        numberOfJumpsRemaining--;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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
            numberOfJumpsRemaining = 2;
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
    }
}
