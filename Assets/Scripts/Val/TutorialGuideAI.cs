using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TutorialGuideAI : MonoBehaviour
{
    [Header("Ruta del Tutorial")]
    [SerializeField]
    private Transform[] waypoints;

    [Header("Control de Espera al Jugador")]
    [SerializeField]
    private Transform playerTransform;

    [SerializeField]
    private float maxPlayerDistance = 4f;

    private NavMeshAgent agent;
    private int currentWaypointIndex;
    private bool isJumping;
    private bool movementStopped;

    private Coroutine traverseLinkCoroutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
    }

    private void Start()
    {
        if (
            movementStopped ||
            waypoints == null ||
            waypoints.Length == 0
        )
        {
            return;
        }

        agent.SetDestination(waypoints[0].position);
    }

    private void Update()
    {
        /*
         * Al presionar Skip ya no se procesa
         * ningún movimiento.
         */
        if (movementStopped)
        {
            return;
        }

        if (
            waypoints == null ||
            waypoints.Length == 0 ||
            currentWaypointIndex >= waypoints.Length
        )
        {
            return;
        }

        /*
         * Comenzar el salto mediante el OffMeshLink.
         */
        if (
            agent.isOnOffMeshLink &&
            !isJumping
        )
        {
            traverseLinkCoroutine = StartCoroutine(
                TraverseLink()
            );
        }

        /*
         * Esperar si el jugador real está demasiado lejos.
         */
        if (
            playerTransform != null &&
            !isJumping
        )
        {
            float distanceToPlayer = Vector3.Distance(
                transform.position,
                playerTransform.position
            );

            agent.isStopped =
                distanceToPlayer > maxPlayerDistance;
        }

        /*
         * Pasar al siguiente waypoint.
         */
        if (
            !agent.pathPending &&
            agent.remainingDistance <=
                agent.stoppingDistance &&
            !isJumping
        )
        {
            currentWaypointIndex++;

            if (currentWaypointIndex < waypoints.Length)
            {
                agent.SetDestination(
                    waypoints[currentWaypointIndex].position
                );
            }
        }
    }

    private IEnumerator TraverseLink()
    {
        isJumping = true;

        OffMeshLinkData data =
            agent.currentOffMeshLinkData;

        Vector3 startPos = agent.transform.position;

        Vector3 endPos =
            data.endPos +
            Vector3.up * agent.baseOffset;

        float duration = 0.25f;
        float timer = 0f;

        while (
            timer < duration &&
            !movementStopped
        )
        {
            float progress = timer / duration;

            Vector3 currentPos = Vector3.Lerp(
                startPos,
                endPos,
                progress
            );

            currentPos.y += Mathf.Sin(
                progress * Mathf.PI
            ) * 1.5f;

            transform.position = currentPos;

            timer += Time.deltaTime;
            yield return null;
        }

        /*
         * Solo completar el salto si no se
         * presionó Skip durante el recorrido.
         */
        if (!movementStopped)
        {
            transform.position = endPos;

            if (
                agent.enabled &&
                agent.isOnNavMesh &&
                agent.isOnOffMeshLink
            )
            {
                agent.CompleteOffMeshLink();
            }
        }

        isJumping = false;
        traverseLinkCoroutine = null;
    }

    /// <summary>
    /// Detiene definitivamente el recorrido del tutorial.
    /// Este método será llamado al presionar Skip.
    /// </summary>
    public void StopMovement()
    {
        movementStopped = true;

        /*
         * Detener el salto si estaba atravesando
         * un OffMeshLink.
         */
        if (traverseLinkCoroutine != null)
        {
            StopCoroutine(traverseLinkCoroutine);
            traverseLinkCoroutine = null;
        }

        isJumping = false;

        /*
         * Detener y borrar la ruta del NavMeshAgent.
         */
        if (
            agent != null &&
            agent.enabled &&
            agent.isOnNavMesh
        )
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        /*
         * Desactivar este comportamiento para
         * impedir nuevos destinos.
         */
        enabled = false;
    }
}