using UnityEngine;

public class TrajectoryLine : MonoBehaviour
{
    [SerializeField] 
    private LineRenderer lineRenderer;

    [SerializeField, Min(3f)]
    private int lineSegments = 60;

    [SerializeField, Min(1f)]
    private float timeOfTheFlight = 5f;

    public void ShowTrajectoryLine(Vector3 startPoint, Vector3 startVelocity, float duration = -1f)
    {
        float flightDuration = duration > 0f ? duration : timeOfTheFlight;
        float timeStep = flightDuration / lineSegments;

        Vector3[] lineRendererPoints = CalculateTrajectoryPoints(startPoint, startVelocity, timeStep);

        lineRenderer.positionCount = lineSegments;
        lineRenderer.SetPositions(lineRendererPoints);
    }

    private Vector3[] CalculateTrajectoryPoints(Vector3 startPoint, Vector3 startVelocity, float timeStep)
    {
        Vector3[] lineRendererPoints = new Vector3[lineSegments];

        lineRendererPoints[0] = startPoint;

        for (int i = 1; i < lineSegments; i++)
        {
            float timeOffset = i * timeStep;

            Vector3 progressBeforeGravity = startVelocity * timeOffset;
            Vector3 gravityOffset = Vector3.up * -0.5f * Physics.gravity.y * timeOffset * timeOffset;
            Vector3 newPosition = startPoint + progressBeforeGravity - gravityOffset;

            lineRendererPoints[i] = newPosition;
        }
        return lineRendererPoints;
    }

}