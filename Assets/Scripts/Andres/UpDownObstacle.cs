using UnityEngine;
using System.Collections;

public class UpDownObstacle : MonoBehaviour
{
    [Header("Time")]
    [SerializeField] private float stayTime = 2f;

    [Header("Movement")]
    [SerializeField] private float obstacleSpeedMovement = 2f;
    [SerializeField] private float yLimit;

    [SerializeField] private TimeManager timeManager;
    private float initialYPosition;
    private float previousTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        initialYPosition = transform.position.y;
        yLimit = transform.position.y + yLimit;
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
                // Guardamos el momento en que empieza la primera fase estatica
                float startTime = timeManager.currentTimeOfGame;

                // Esperar hasta que pase la primera fase estatica
                yield return WaitForGameTime(startTime, stayTime);

                // Moverse hasta la posicion Y
                yield return MoveToYPosition(yLimit);

                // Guardamos el momento en que empieza la segunda fase estatica
                startTime = timeManager.currentTimeOfGame;

                // Esperar hasta que pase la segunda fase estatica
                yield return WaitForGameTime(startTime, stayTime);

                // Volver a la posición inicial
                yield return MoveToYPosition(initialYPosition);
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

    private IEnumerator MoveToYPosition(float targetYPosition)
    {
        Vector3 targetPosition = new Vector3(
            transform.position.x,
            targetYPosition,
            transform.position.z
        );

        previousTime = timeManager.currentTimeOfGame;

        while (!Mathf.Approximately(transform.position.y, targetYPosition))
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
