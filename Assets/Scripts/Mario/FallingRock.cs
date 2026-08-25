using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallingRock : MonoBehaviour
{
    [Header("Caída")]
    [SerializeField, Min(0.1f)] private float fallSpeed = 40f; // Velocidad inicial de caída (unidades/segundo)
    [SerializeField, Min(0f)] private float fallAcceleration = 60f; // Cuánto acelera la caída por segundo mientras sigue en el aire
    [SerializeField, Min(0.1f)] private float mass = 5f; // Peso de la roca; afecta el impulso que transmite al golpear algo

    [Header("Giro")]
    // El giro es puramente visual: se le da una velocidad angular y ahí se queda. No
    // desvía la caída, porque sin gravedad ni rozamiento en el aire una rotación no
    // genera desplazamiento por sí sola, y al tocar el suelo la roca se detiene.
    [Tooltip("Grados por segundo mínimos de giro. Cada roca sortea el suyo entre este valor y el máximo, para que no haya dos cayendo igual.")]
    [SerializeField, Min(0f)] private float minSpinSpeed = 90f;

    [Tooltip("Grados por segundo máximos de giro.")]
    [SerializeField, Min(0f)] private float maxSpinSpeed = 260f;

    [Tooltip("Sortea también la orientación de partida. Sin esto todas las rocas aparecen en la misma pose y se nota que son la misma pieza repetida.")]
    [SerializeField] private bool randomizeInitialRotation = true;

    [Header("Impacto")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask groundLayer; // Layer "Ground"
    [SerializeField, Min(1)] private int blinkCount = 2;
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.15f;
    [SerializeField, Min(1f)] private float maxLifeTime = 8f; // Respaldo: se destruye sola si no impacta con nada, para que no se acumulen en la escena

    private Renderer[] renderers;
    private Rigidbody rb;
    private Collider rockCollider;
    private bool isDisappearing;

    [SerializeField] private bool canFall = true;
    private GameManager gameManager;
    private Vector3 velocityBeforePause;
    private Vector3 angularVelocityBeforePause;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        rb = GetComponent<Rigidbody>();
        rockCollider = GetComponent<Collider>();

        // La caída se controla a mano (velocidad inicial + aceleración propia) en vez de
        // depender de la gravedad del proyecto, para poder ajustarla sin afectar
        // al jugador ni a otros objetos.
        rb.useGravity = false;
        rb.mass = mass;
        rb.linearVelocity = Vector3.down * fallSpeed;

        // Una roca que nazca ya congelada (por una pausa o una escena en curso)
        // nunca llega a pasar por DisableFall, asi que sin esto se reanudaria
        // desde cero en vez de con la velocidad de caida que le tocaba.
        velocityBeforePause = rb.linearVelocity;

        // Evita que la roca atraviese colliders delgados (como un suelo plano)
        // al caer rápido, comprobando colisiones de forma continua.
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        StartSpinning();

        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnGamePaused += DisableFall;
            gameManager.OnGameFinished += DisableFall;
            gameManager.OnGameResumed += EnableFall;
            gameManager.OnGameOver += DestroyRock;

            // No basta con dejar de generar rocas: las que ya estan en el aire llegarian
            // igual, y con el mismo resultado injusto
            gameManager.OnCutsceneStarted += DisableFall;
            gameManager.OnCutsceneEnded += EnableFall;
        }

        canFall = !(gameManager.isGamePaused || gameManager.gameFinished || gameManager.gameOver || gameManager.isCutscenePlaying);

    }

    private void Start()
    {
    }

    private void Update()
    {
        if (isDisappearing) return;
        if (canFall)
        {
            maxLifeTime -= Time.deltaTime;
            if(maxLifeTime <= 0f)
            {
                Destroy(gameObject);
            }    
        }
    }

    private void FixedUpdate()
    {
        if (isDisappearing) return;

        if (canFall)
        {
            rb.linearVelocity += Vector3.down * fallAcceleration * Time.fixedDeltaTime;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;

            // Mientras está en pausa el giro se sostiene en cero igual que la caída:
            // un choque durante la pausa podría reavivarlo y se vería rotar sola
            rb.angularVelocity = Vector3.zero;
        }

        
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGamePaused -= DisableFall;
            gameManager.OnGameFinished -= DisableFall;
            gameManager.OnGameResumed -= EnableFall;
            gameManager.OnGameOver -= DestroyRock;
            gameManager.OnCutsceneStarted -= DisableFall;
            gameManager.OnCutsceneEnded -= EnableFall;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (isDisappearing) return;

        bool hitPlayer = collision.gameObject.CompareTag(playerTag);
        bool hitGround = (groundLayer.value & (1 << collision.gameObject.layer)) != 0;

        if (!hitPlayer && !hitGround) return;

        if (hitPlayer)
            collision.gameObject.SendMessage("OnRockImpact", SendMessageOptions.DontRequireReceiver);

        StartCoroutine(BlinkAndDestroy());
    }

    private IEnumerator BlinkAndDestroy()
    {
        isDisappearing = true;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        rockCollider.enabled = false;

        for (int i = 0; i < blinkCount; i++)
        {
            SetRenderersVisible(false);
            yield return new WaitForSeconds(blinkInterval);
            SetRenderersVisible(true);
            yield return new WaitForSeconds(blinkInterval);
        }

        Destroy(gameObject);
    }

    private void SetRenderersVisible(bool isVisible)
    {
        foreach (Renderer rockRenderer in renderers)
            rockRenderer.enabled = isVisible;
    }

    // Le da a la roca su giro propio: un eje cualquiera y una velocidad sorteada.
    // Random.onUnitSphere reparte los ejes por igual en todas las direcciones, así que
    // unas caen volteando de frente y otras de lado en lugar de girar todas igual.
    private void StartSpinning()
    {
        if (randomizeInitialRotation)
        {
            transform.rotation = Random.rotation;
        }

        float spinSpeed = Random.Range(minSpinSpeed, maxSpinSpeed);

        // Unity recorta la velocidad angular a 7 rad/s (unos 400 grados/s) por defecto,
        // así que sin subir el tope un valor alto en el Inspector se quedaría corto sin
        // decir nada. Se sube al máximo configurado para que valga lo que se escriba.
        rb.maxAngularVelocity = Mathf.Max(rb.maxAngularVelocity, maxSpinSpeed * Mathf.Deg2Rad);

        // El rozamiento angular frenaría el giro poco a poco. Se anula por el mismo
        // motivo que la gravedad: aquí la caída se controla a mano, de principio a fin.
        rb.angularDamping = 0f;

        rb.angularVelocity = Random.onUnitSphere * (spinSpeed * Mathf.Deg2Rad);
    }

    public void EnableFall()
    {
        canFall = true;
        rb.linearVelocity = velocityBeforePause;

        // El giro también se congela al pausar, así que hay que devolvérselo: si no,
        // la roca se reanudaría cayendo pero quieta, como un bloque rígido.
        rb.angularVelocity = angularVelocityBeforePause;
    }

    public void DisableFall()
    {
        canFall = false;
        velocityBeforePause = rb.linearVelocity;
        angularVelocityBeforePause = rb.angularVelocity;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void DestroyRock()
    {
        Destroy(gameObject);
    }
}

