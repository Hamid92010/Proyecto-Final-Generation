using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private LayerMask obstacleLayer;

    public Transform Target { get; private set; }
    public bool HasTarget => Target != null;
    public bool HasLineOfSight { get; private set; }

    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
            Target = player.transform;
    }

    private void Update()
    {
        HasLineOfSight = CheckLineOfSight();
    }

    private bool CheckLineOfSight()
    {
        if (Target == null) return false;

        Vector3 directionToTarget = Target.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > detectionRange)
            return false;

        if (Physics.Raycast(transform.position, directionToTarget.normalized, out RaycastHit hit, detectionRange, obstacleLayer))
        {
            Debug.DrawLine(transform.position, hit.point, Color.red);
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = HasLineOfSight ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}