using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        Vector3 direction = mainCamera.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
    }
}