using UnityEngine;
using UnityEngine.InputSystem;

// Retiramos el RequireComponent del Animator. Solo exigimos el Rigidbody porque el script vive en el Padre.
[RequireComponent(typeof(Rigidbody))]
public class PlayerAnimationBridge : MonoBehaviour
{
    [Header("Referencias Principales")]
    [Tooltip("Arrastra aquí el objeto HIJO (el modelo del pollito) que contiene el Animator")]
    [SerializeField] private Animator animator;
    
    private Rigidbody rb;

    [Header("Configuración de Input")]
    [Tooltip("Arrastra aquí el mismo InputActionAsset que usa el PlayerMovement")]
    [SerializeField] private InputActionAsset inputActions;
    private InputAction jumpAction;

    [Header("Ground Check (Independiente)")]
    [Tooltip("Copia los mismos valores que tienes en PlayerMovement para sincronía exacta")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector3 groundCheckSize = new Vector3(0.8f, 0.1f, 0.8f);
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;

    // Hashes para optimizar el rendimiento del Animator
    private readonly int hashIsGrounded = Animator.StringToHash("IsGrounded");
    private readonly int hashHorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
    private readonly int hashVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private readonly int hashJumpTrigger = Animator.StringToHash("JumpTrigger");

    private void Awake()
    {
        // El Rigidbody se obtiene directamente del Padre
        rb = GetComponent<Rigidbody>();
        
        // Reflexión técnica de seguridad: Si olvidas arrastrar el Animator en el inspector, 
        // el código lo buscará automáticamente iterando a través de los objetos hijos.
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            
            if (animator == null)
            {
                Debug.LogError("CRÍTICO: No se encontró ningún Animator en los hijos del jugador. Revisa la jerarquía.", this);
            }
        }
        
        if (inputActions != null)
        {
            jumpAction = inputActions.FindAction("Jump");
        }
    }

    private void OnEnable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed += OnJumpPerformed;
            jumpAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.Disable();
        }
    }

    private void Update()
    {
        // Detener la ejecución si no hay animator (evita saturar la consola de errores NullReference)
        if (animator == null) return;

        // 1. Evaluación Independiente del Suelo
        isGrounded = Physics.CheckBox(
            groundCheck.position + Vector3.down * groundCheckDistance, 
            groundCheckSize / 2f, 
            Quaternion.identity, 
            groundLayer
        );

        UpdateAnimatorParameters();
    }

    private void UpdateAnimatorParameters()
    {
        // 2. Sincronizar estado
        animator.SetBool(hashIsGrounded, isGrounded);

        // 3. Sincronizar Velocidad Horizontal
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        animator.SetFloat(hashHorizontalSpeed, horizontalVelocity.magnitude);

        // 4. Sincronizar Velocidad Vertical pura
        animator.SetFloat(hashVerticalVelocity, rb.linearVelocity.y);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (animator != null)
        {
            animator.SetTrigger(hashJumpTrigger);
        }
    }
}