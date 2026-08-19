using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float lifeTime = 5f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Se fuerza a que sea un trigger para garantizar que OnTriggerEnter
        // se dispare sin importar cómo esté configurado el collider en el prefab.
        Collider projectileCollider = GetComponent<Collider>();
        if (projectileCollider != null)
            projectileCollider.isTrigger = true;
    }

    public void Launch(Vector3 velocity)
    {
        rb.useGravity = false;
        rb.linearVelocity = velocity;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador golpeado");
        }

        Destroy(gameObject);
    }
}