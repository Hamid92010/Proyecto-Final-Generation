using UnityEngine;

// Marca en el suelo que avisa dónde va a caer una roca. Sigue al jugador
// en todo momento (incluso si sube escalones/plataformas) y parpadea
// para indicar peligro, hasta que el spawner la destruye.
public class RockWarningIndicator : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.05f, 1.5f); // Desplazamiento respecto al jugador (X/Z para ponerlo al frente, Y para separarlo del suelo)
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.2f; // Velocidad del parpadeo de aviso

    private Transform target;
    private LayerMask groundLayer;
    private Renderer[] renderers;
    private float blinkTimer;
    private bool isVisible = true;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void Follow(Transform newTarget, LayerMask newGroundLayer)
    {
        target = newTarget;
        groundLayer = newGroundLayer;
    }

    private void LateUpdate()
    {
        if (target != null)
            transform.position = GetGroundPosition(target.position);

        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
            isVisible = !isVisible;
            SetRenderersVisible(isVisible);
        }
    }

    private Vector3 GetGroundPosition(Vector3 fromPosition)
    {
        Vector3 probePosition = fromPosition + new Vector3(offset.x, 0f, offset.z);

        if (Physics.Raycast(probePosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
            return hit.point + Vector3.up * offset.y;

        return probePosition;
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (Renderer r in renderers)
            r.enabled = visible;
    }
}
