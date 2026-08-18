using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float offsetY;

    private void LateUpdate()
    {
        transform.position = new Vector3( transform.position.x, player.position.y + offsetY,transform.position.z);
    }
}