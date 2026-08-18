using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
            Rigidbody playerRb = player.GetComponent<Rigidbody>();

            Vector3 knockbackDirection = collision.transform.position - transform.position;
            knockbackDirection.y = 0f;
            knockbackDirection.Normalize();

            playerRb.AddForce(
                knockbackDirection * knockbackForce,
                ForceMode.Impulse
            );

            player.StartCoroutine(player.KnockbackCooldown(knockbackDuration));
        }
    }
}