using UnityEngine;
using System.Collections;

public class HidingObstacle : MonoBehaviour
{
    [Header("Time")]
    [SerializeField] private float visibleTime = 2f;
    [SerializeField] private float hiddenTime = 1f;

    [Header("Movement")]
    [SerializeField] private float obstacleSpeedMovement = 2f;
    [SerializeField] private float hiddenZPosition;

    [SerializeField] private TimeManager timeManager;
    private float initialZPosition;
    private float previousTime;

    private void Awake()
    {
        initialZPosition = transform.position.z;
    }

    private void OnEnable()
    {
        if (timeManager == null)
        {
            timeManager = FindAnyObjectByType<TimeManager>();
        }
    }
    private void Start()
    {
        StartCoroutine(ObstacleCycle());
    }

    private IEnumerator ObstacleCycle()
    {
        while (true)
        {
            while (true)
            {
                // Guardamos el momento en que empieza la fase visible
                float startTime = timeManager.currentTimeOfGame;

                // Esperar hasta que pase visibleTime
                yield return WaitForGameTime(startTime, visibleTime);

                // Moverse hasta la posición escondida
                yield return MoveToZPosition(hiddenZPosition);

                // Guardamos el momento en que empieza la fase escondida
                startTime = timeManager.currentTimeOfGame;

                // Esperar hasta que pase hiddenTime
                yield return WaitForGameTime(startTime, hiddenTime);

                // Volver a la posición inicial
                yield return MoveToZPosition(initialZPosition);
            }
        }
    }

    private IEnumerator WaitForGameTime(float startTime, float duration)
    {
        while (timeManager.currentTimeOfGame - startTime < duration)
        {
            yield return null;
        }
    }

    private IEnumerator MoveToZPosition(float targetZPosition)
    {
        Vector3 targetPosition = new Vector3(
            transform.position.x,
            transform.position.y,
            targetZPosition
        );

        previousTime = timeManager.currentTimeOfGame;

        while (!Mathf.Approximately(transform.position.z, targetZPosition))
        {
            float currentTime = timeManager.currentTimeOfGame;
            float elapsedTime = currentTime - previousTime;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                obstacleSpeedMovement * elapsedTime
            );

            previousTime = currentTime;

            yield return null;
        }
    }
}

