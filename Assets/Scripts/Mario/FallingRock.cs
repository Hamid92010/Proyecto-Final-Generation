using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallingRock : MonoBehaviour
{
    [Header("Caída")]
    [SerializeField, Min(0.1f)] private float fallSpeed = 40f; // Velocidad inicial de caída (unidades/segundo)
    [SerializeField, Min(0f)] private float fallAcceleration = 60f; // Cuánto acelera la caída por segundo mientras sigue en el aire
    [SerializeField, Min(0.1f)] private float mass = 5f; // Peso de la roca; afecta el impulso que transmite al golpear algo

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

        // Evita que la roca atraviese colliders delgados (como un suelo plano)
        // al caer rápido, comprobando colisiones de forma continua.
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Start()
    {
        Destroy(gameObject, maxLifeTime);
    }

    private void FixedUpdate()
    {
        if (isDisappearing) return;

        rb.linearVelocity += Vector3.down * fallAcceleration * Time.fixedDeltaTime;
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
}
