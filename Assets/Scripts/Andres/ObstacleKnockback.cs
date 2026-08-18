using UnityEngine;

public class ObstacleKnockback : MonoBehaviour
{
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
            Rigidbody playerRb = player.GetComponent<Rigidbody>();

            // Obtener la superficie que golpeó el jugador
            Vector3 collisionNormal = collision.contacts[0].normal;

            Debug.Log($"Normal: {collisionNormal}");
            // Solo queremos el componente horizontal
            Vector3 knockbackDirection = new Vector3(-collisionNormal.x, 0f, 0f);

            Debug.Log($"Knockback Direction: {knockbackDirection}");
            // Normalizar por seguridad
            knockbackDirection.Normalize();

            // Mantener la velocidad vertical actual
            float verticalVelocity = playerRb.linearVelocity.y;

            // Si el jugador está subiendo, cancelar la subida
            if (verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }

            // Aplicar el knockback horizontal
            playerRb.linearVelocity = new Vector3(
                knockbackDirection.x * knockbackForce,
                verticalVelocity,
                playerRb.linearVelocity.z
            );

            // Bloquear temporalmente el movimiento del jugador
            player.StartCoroutine(
                player.KnockbackCooldown(knockbackDuration)
            );
        }
    }
}