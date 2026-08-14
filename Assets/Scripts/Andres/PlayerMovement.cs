using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;


public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset InputActions;
    private InputAction m_moveAction;
    private InputAction m_jumpAction;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float fallForce = 0f;
    [SerializeField] private int numberOfJumpsRemaining = 2;

    private Rigidbody rb;
    private Vector2 moveInput;
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        m_moveAction = InputActions.FindAction("Move");
        m_jumpAction = InputActions.FindAction("Jump");
    }

    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
        gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.OnGameOver += StopMovement;
            gameManager.OnGameFinished += StopMovement;
            gameManager.OnGamePaused += StopMovement;
            gameManager.OnGameResumed += ResumeMovement;
        }

        if(playerCollisions != null)
        {
            playerCollisions.TouchObstacle += TouchObstacle;
            playerCollisions.UnTouchObstacle += UnTouchObstacle;
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
            gameManager.OnGamePaused -= StopMovement;
            gameManager.OnGameResumed -= ResumeMovement;
        }

        if (playerCollisions != null)
        {
            playerCollisions.TouchObstacle -= TouchObstacle;
            playerCollisions.UnTouchObstacle -= UnTouchObstacle;
        }
    }

    private void Update()
    {
        if(!canPlayerMove)
        {
            return;
        }

        moveInput = m_moveAction.ReadValue<Vector2>();
        if (moveInput.x != 0 && !gameManager.isGameStarted)
        {
            gameManager.StartGame();

        }
        isGrounded = Physics.CheckBox( groundCheck.position + Vector3.down * groundCheckDistance, groundCheckSize / 2f, Quaternion.identity, groundLayer);
        if(isGrounded && !wasGrounded)
        {
            numberOfJumpsRemaining = 2;
        }

        wasGrounded = isGrounded;

        if (m_jumpAction.WasPressedThisFrame() && (isGrounded || numberOfJumpsRemaining > 0))
        {           
            Jump();           
        }


    }

    private void FixedUpdate()
    {
        Vector3 movement = new Vector3( moveInput.x, 0f, 0f);

        if (!isTouchingObstacle || isGrounded)
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }

        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * fallForce, ForceMode.Acceleration);
        }
        // Rotación
        if (movement != Vector3.zero)
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

    private void OnDrawGizmos()
    {
        if (groundCheck == null)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheck.position + Vector3.down * groundCheckDistance, groundCheckSize);
    }
}
