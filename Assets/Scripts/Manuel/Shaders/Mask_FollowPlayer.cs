using UnityEngine;

public class Mask_FollowPlayer : MonoBehaviour
{
    //Declare variables
    [SerializeField] Transform playerTransform,spriteToCut;
    private Vector3 playerPosition = new Vector3(0, 0, 0);
    private Vector3 gridPositionZ = new Vector3(0, 0, 0);
    [SerializeField] float offsetX = 0f, offsetY = 0f, offsetZ = 0f;

    void Start()
    {
        gridPositionZ = spriteToCut.position;
    }


    // Update is called once per frame
    void LateUpdate()
    {
        playerPosition = playerTransform.position;

        transform.position = new Vector3(playerPosition.x, playerPosition.y, playerPosition.z);
    }
}
