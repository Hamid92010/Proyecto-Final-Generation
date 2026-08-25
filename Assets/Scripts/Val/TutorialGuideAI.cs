using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TutorialGuideAI : MonoBehaviour
{
    [Header("Ruta del Tutorial")]
    [SerializeField] private Transform[] waypoints; 

    [Header("Control de Espera al Jugador")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float maxPlayerDistance = 4f; 

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private bool isJumping = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        agent.autoTraverseOffMeshLink = false; 
    }

    private void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[0].position);
        }
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0 || currentWaypointIndex >= waypoints.Length)
            return;

        
        if (agent.isOnOffMeshLink && !isJumping)
        {
            StartCoroutine(TraverseLink());
        }

        
        if (playerTransform != null && !isJumping)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            agent.isStopped = (distanceToPlayer > maxPlayerDistance);
        }

        
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !isJumping)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex < waypoints.Length)
            {
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
        }
    }

    
    private IEnumerator TraverseLink()
    {
        isJumping = true;
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;

        float duration = 0.6f; 
        float timer = 0f;

        while (timer < duration)
        {
            float progress = timer / duration;
            
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 1.5f; 

            transform.position = currentPos;
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        agent.CompleteOffMeshLink();
        isJumping = false;
    }
}