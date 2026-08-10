using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset InputActions;
    private InputAction m_moveAction;
    private InputAction m_jumpAction;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float fallForce = 0f;

    private Rigidbody rb;
    private Vector2 moveInput;
    [SerializeField] private bool isGrounded = true;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        m_moveAction = InputActions.FindAction("Move");
        m_jumpAction = InputActions.FindAction("Jump");
    }

    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }

    private void Update()
    {
        moveInput = m_moveAction.ReadValue<Vector2>();
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, groundLayer);
        if (m_jumpAction.WasPressedThisFrame() && isGrounded)
        {
            Jump();
        }


    }

    private void FixedUpdate()
    {
        Vector3 movement = new Vector3( moveInput.x, 0f, moveInput.y);

        //rb.MovePosition( rb.position + movement * moveSpeed * Time.fixedDeltaTime );

        if(isGrounded)
        {
            rb.linearVelocity = new Vector3(movement.x * moveSpeed, rb.linearVelocity.y, movement.z * moveSpeed);
        }else
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
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
}
