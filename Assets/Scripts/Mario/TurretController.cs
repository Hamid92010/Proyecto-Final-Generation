using UnityEngine;

[RequireComponent(typeof(PlayerDetector))]
public class TurretController : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab; // Prefab del proyectil que se disparará
    [SerializeField] private Transform muzzle; // Punto de disparo del proyectil
    [SerializeField, Min(1f)] private float projectileMass = 30f; // Masa del proyectil, usada para calcular la velocidad de lanzamiento
    [SerializeField, Min(0.1f)] private float shootForce = 30f; // Fuerza de disparo, usada para calcular la velocidad de lanzamiento
    [SerializeField, Min(0.1f)] private float fireRate = 1f; // Frecuencia de disparo en segundos
    [SerializeField] private float shootAngle = 45f; // Ángulo de disparo
    [SerializeField] private float rotationSpeed = 5f; // Velocidad de rotación

    [SerializeField] private TrajectoryLine trajectoryLine;
    [SerializeField] private PlayerDetector detector;

    private float fireTimer;

    private void Awake()
    {
        if (detector == null)
            detector = GetComponent<PlayerDetector>();
    }

    void Update()
{
    if (!detector.HasTarget) return;

    if (detector.HasLineOfSight)
    {
        AimAtTarget(detector.Target);

        Vector3 velocity = GetLaunchVelocity();

        if (trajectoryLine != null)
        {
            float distance = Vector3.Distance(muzzle.position, detector.Target.position);
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            float estimatedTime = horizontalVelocity.magnitude > 0.01f
                ? distance / horizontalVelocity.magnitude
                : 1f;

            trajectoryLine.ShowTrajectoryLine(muzzle.position, velocity, estimatedTime * 1.3f);
        }

        HandleFiring();
    }
}

    void AimAtTarget(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
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
        if (projectilePrefab == null || muzzle == null) return;
        Debug.Log("Fire() llamado, velocidad: " + GetLaunchVelocity());

        GameObject projectileObj = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        projectile.Launch(GetLaunchVelocity());
    }

    // Combina la dirección del cañón con el ángulo de disparo y calcula
    // la velocidad inicial a partir de shootForce/projectileMass.
    // Esto es lo mismo que ve la línea de trayectoria, así que ambas coinciden.
    Vector3 GetLaunchVelocity()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(muzzle.forward, Vector3.up).normalized;
        Vector3 pitchedDirection = Quaternion.AngleAxis(shootAngle, transform.right) * flatForward;
        return pitchedDirection.normalized * (shootForce / projectileMass);
    }
}