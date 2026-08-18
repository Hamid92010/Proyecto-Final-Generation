using UnityEngine;

public class TrajectoryLine : MonoBehaviour
{
    [SerializeField]
    private LineRenderer lineRenderer;

    public void ShowLineToTarget(Vector3 startPoint, Vector3 targetPoint)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, targetPoint);
    }
}
