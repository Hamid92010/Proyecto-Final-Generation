using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador golpeado");
            // Aquí después conectamos daño/game over
        }

        Destroy(gameObject);
    }
}
