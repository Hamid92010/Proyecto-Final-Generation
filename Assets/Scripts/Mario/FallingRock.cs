using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallingRock : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask groundLayer; // Layer "Ground"
    [SerializeField, Min(1)] private int blinkCount = 2;
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.15f;

    private Renderer[] renderers;
    private Rigidbody rb;
    private Collider rockCollider;
    private bool isDisappearing;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        rb = GetComponent<Rigidbody>();
        rockCollider = GetComponent<Collider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDisappearing) return;

        bool hitPlayer = collision.gameObject.CompareTag(playerTag);
        bool hitGround = (groundLayer.value & (1 << collision.gameObject.layer)) != 0;

        if (hitPlayer || hitGround)
            StartCoroutine(BlinkAndDestroy());
    }

    private IEnumerator BlinkAndDestroy()
    {
        isDisappearing = true;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        rockCollider.enabled = false;

        for (int i = 0; i < blinkCount; i++)
        {
            SetRenderersVisible(false);
            yield return new WaitForSeconds(blinkInterval);
            SetRenderersVisible(true);
            yield return new WaitForSeconds(blinkInterval);
        }

        Destroy(gameObject);
    }

    private void SetRenderersVisible(bool isVisible)
    {
        foreach (Renderer rockRenderer in renderers)
            rockRenderer.enabled = isVisible;
    }
}
