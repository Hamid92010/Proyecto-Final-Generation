using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform target;        // Arrastra aquí al Player
    public Transform firePoint;     // El punto desde donde dispara

    [Header("Apuntado")]
    public float rotationSpeed = 3f;

    [Header("Detección")]
    public float detectionRange = 20f;
    public LayerMask obstacleLayer; // Layers que bloquean la línea de visión (paredes, etc.)

    [Header("Disparo")]
    public float fireRate = 1.5f;   // Segundos entre disparos
    public GameObject projectilePrefab;

    private bool hasLineOfSight;
    private float fireTimer;

    void Update()
    {
        if (target == null) return;

        CheckLineOfSight();

        if (hasLineOfSight)
        {
            AimAtTarget();
            HandleFiring();
        }
    }

    void CheckLineOfSight()
    {
        Vector3 directionToTarget = target.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > detectionRange)
        {
            hasLineOfSight = false;
            return;
        }

        // Raycast desde la torreta hacia el jugador
        if (Physics.Raycast(transform.position, directionToTarget.normalized, out RaycastHit hit, detectionRange, obstacleLayer))
        {
            // Si el raycast pega contra algo que NO es el jugador, hay un obstáculo bloqueando
            hasLineOfSight = false;
        }
        else
        {
            hasLineOfSight = true;
        }
    }

    void AimAtTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // Si quieres que solo rote en el eje horizontal, no que se incline

        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    void HandleFiring()
    {
        fireTimer += Time.deltaTime;

        if (fireTimer >= fireRate)
        {
            Fire();
            fireTimer = 0f;
        }
    }

    void Fire()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    }

    // Visualiza el rango de detección en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = hasLineOfSight ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}