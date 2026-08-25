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
    public float warningDuration = 2f; // Tiempo que el aviso sigue al jugador antes de que caiga la roca
    private GameManager gameManager;
    private bool canBlink = true;
    private RockSpawner rockSpawner;
    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        gameManager = FindAnyObjectByType<GameManager>();
        rockSpawner = FindAnyObjectByType<RockSpawner>();
    }

    public void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnGamePaused += DisableBlink;
            gameManager.OnGameFinished += DisableBlink;
            gameManager.OnGameOver += DisableBlink;
            gameManager.OnGameResumed += EnableBlink;
        }
    }
    public void Follow(Transform newTarget, LayerMask newGroundLayer)
    {
        target = newTarget;
        groundLayer = newGroundLayer;
    }

    //Andres
    private void Update()
    {
        if (gameManager != null && canBlink)
        {
           warningDuration -= Time.deltaTime;
        }

        if(warningDuration <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (!canBlink)
        {
            return;
        }

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

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGamePaused -= DisableBlink;
            gameManager.OnGameFinished -= DisableBlink;
            gameManager.OnGameOver -= DisableBlink;
            gameManager.OnGameResumed -= EnableBlink;
        }
    }


    private Vector3 GetGroundPosition(Vector3 fromPosition)
    {


        return fromPosition + offset;

        /* Vector3 probePosition = fromPosition + new Vector3(offset.x, 0f, offset.z);

        if (Physics.Raycast(probePosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
            return hit.point + Vector3.up * offset.y;

        return probePosition;*/

    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (Renderer r in renderers)
            r.enabled = visible;
    }

    public void EnableBlink()
    {
        canBlink = true;
    }

    public void DisableBlink()
    {
        canBlink = false;
    }

    private void OnDestroy()
    {
        // Este indicador también se destruye al descargar la escena (game over,
        // victoria, cambio de nivel), no solo cuando expira su temporizador.
        // En ese caso el player y el propio rockSpawner ya pueden estar
        // destruidos, así que no debe intentar generar una roca.
        if (gameManager != null && (gameManager.gameOver || gameManager.gameFinished))
            return;

        if (rockSpawner != null)
        {
            rockSpawner.SpawnRock();
        }
    }
}
