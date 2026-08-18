using UnityEngine;

[RequireComponent(typeof(PlayerDetector))]
public class TurretController : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab; // Prefab del proyectil que se disparará
    [SerializeField] private Transform muzzle; // Punto de disparo del proyectil
    [SerializeField, Min(0.1f)] private float shootSpeed = 40f; // Velocidad del disparo directo
    [SerializeField, Min(0.1f)] private float fireRate = 1f; // Frecuencia de disparo en segundos
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

            if (trajectoryLine != null)
                trajectoryLine.ShowLineToTarget(muzzle.position, detector.Target.position);

            HandleFiring();
        }
    }

    // Apunta el cañón directamente al jugador (yaw + pitch), sin importar
    // la diferencia de altura, para que siga apuntando aunque suba escalones o plataformas.
    void AimAtTarget(Transform target)
    {
        Vector3 direction = target.position - transform.position;
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
        if (projectilePrefab == null || muzzle == null || !detector.HasTarget) return;

        Vector3 direction = (detector.Target.position - muzzle.position).normalized;
        GameObject projectileObj = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(direction));
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        projectile.Launch(direction * shootSpeed);
    }
}
