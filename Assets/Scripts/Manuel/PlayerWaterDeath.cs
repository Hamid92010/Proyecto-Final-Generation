using System.Collections;
using UnityEngine;

// ---------------------------------------------------------------------------
// Secuencia de ahogamiento de Bravard.
//
// Antes, al tocar el agua todo se congelaba de golpe: el personaje se quedaba
// clavado en la superficie mientras la animación de muerte sonaba de fondo, y
// no se leía nada. Aquí se le devuelve el movimiento durante esos segundos, pero
// un movimiento COREOGRAFIADO, no físico: sale a flote, se mece (sube, baja,
// sube, baja) y al final se hunde. En paralelo gira para mirar a la cámara.
//
// El Rigidbody se pone en kinematic durante toda la secuencia. Es lo que permite
// atravesar el collider del agua al hundirse: un cuerpo dinámico chocaría contra
// la superficie y se quedaría encima por mucho que se le pidiera bajar.
//
// Este componente va en el objeto PLAYER, junto a PlayerMovement.
// ---------------------------------------------------------------------------
[RequireComponent(typeof(Rigidbody))]
public class PlayerWaterDeath : MonoBehaviour
{
    [Header("Salida a flote")]
    // El punto de impacto NO sirve como línea de flotación: al chocar, Bravard queda
    // hundido hasta la altura del agua y la animación no se ve. Esta es la altura a la
    // que emerge para quedar por encima de la superficie, medida desde ese impacto.
    [Tooltip("Cuánto emerge Bravard sobre el punto donde tocó el agua. Súbelo si la animación sigue quedando por debajo de la superficie.")]
    [SerializeField] private float floatHeight = 0.8f;

    [Tooltip("Segundos que tarda en salir a flote. Es el empujón inicial del agua, así que conviene que sea corto.")]
    [SerializeField] private float riseDuration = 0.5f;

    [Header("Flotación")]
    [Tooltip("Segundos que Bravard pasa meciéndose en la superficie antes de hundirse.")]
    [SerializeField] private float bobDuration = 2.5f;

    [Tooltip("Vaivenes completos (una subida y una bajada cada uno) durante la flotación.")]
    [SerializeField] private float bobCycles = 2f;

    [Tooltip("Cuánto sube y baja respecto a la línea de flotación, en unidades.")]
    [SerializeField] private float bobAmplitude = 0.35f;

    [Header("Hundimiento")]
    [Tooltip("Segundos que tarda en irse al fondo una vez deja de flotar.")]
    [SerializeField] private float sinkDuration = 2f;

    [Tooltip("Unidades que desciende desde la línea de flotación. Tiene que bastar para que se pierda de vista bajo el agua.")]
    [SerializeField] private float sinkDepth = 3f;

    [Header("Giro hacia la cámara")]
    [Tooltip("Segundos que tarda en encarar la cámara. Corre en paralelo al resto: empieza con la salida a flote, no después.")]
    [SerializeField] private float turnToCameraDuration = 0.6f;

    // Cuerpo al que se le coreografía el descenso
    private Rigidbody rb;

    // Quien avisa de que la partida se ha perdido y por qué motivo
    private GameManager gameManager;

    // Lo que dura la secuencia entera. Es el valor que tiene que caber dentro del
    // gameOverLapTime del UIManager para que el corte a la pantalla de derrota no
    // llegue antes de que Bravard termine de hundirse.
    public float TotalDuration => riseDuration + bobDuration + sinkDuration;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // Mismo patrón de búsqueda que sigue el resto del proyecto
        gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.OnGameOverCaused += HandleGameOver;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameOverCaused -= HandleGameOver;
        }
    }

    // Solo el agua ahoga. Perder por tiempo tiene su propia animación y ocurre en
    // tierra firme, así que ahí no hay nada que coreografiar.
    private void HandleGameOver(GameOverCause cause)
    {
        if (cause != GameOverCause.Water)
        {
            return;
        }

        WarnIfSceneCutsTooSoon();

        StartCoroutine(DrownSequence());
    }

    // La secuencia y el corte a la pantalla de derrota son dos relojes distintos que
    // arrancan a la vez, así que un descuadre entre ellos se traduce en un hundimiento
    // que nadie llega a ver. Es justo el fallo que costó encontrar la primera vez, y
    // por eso se avisa en lugar de dejarlo pasar en silencio.
    private void WarnIfSceneCutsTooSoon()
    {
        UIManager uiManager = FindAnyObjectByType<UIManager>();

        if (uiManager != null && uiManager.GameOverLapTime < TotalDuration)
        {
            Debug.LogWarning($"El ahogamiento dura {TotalDuration}s pero la pantalla de derrota entra a los {uiManager.GameOverLapTime}s: el hundimiento se cortará a medias. Sube gameOverLapTime en el UIManager.", this);
        }
    }

    private IEnumerator DrownSequence()
    {
        // A partir de aquí la altura la manda este script y nadie más: ni la gravedad,
        // ni el empuje del agua, ni el impulso que Bravard trajera al caer.
        rb.isKinematic = true;

        // Donde el agua le paró. Sirve de referencia, no de línea de flotación: sobre
        // este punto se levanta floatHeight para que el cuerpo quede a la vista.
        Vector3 impactPosition = rb.position;
        Vector3 floatPosition = impactPosition + Vector3.up * floatHeight;

        // El giro NO se encadena con yield: tiene que ocurrir mientras emerge, no
        // después. Por eso va suelto, con su propio reloj, en paralelo al resto.
        StartCoroutine(TurnToCamera());

        yield return StartCoroutine(RiseToSurface(impactPosition, floatPosition));

        yield return StartCoroutine(FloatOnSurface(floatPosition));

        yield return StartCoroutine(SinkUnderwater(floatPosition));
    }

    // La subida inicial hasta la línea de flotación. Con SmoothStep frena al llegar,
    // que es como emerge algo empujado por el agua; un ascenso lineal se pararía en
    // seco justo donde empieza el vaivén y se notaría el corte entre las dos fases.
    private IEnumerator RiseToSurface(Vector3 impactPosition, Vector3 floatPosition)
    {
        float elapsedTime = 0f;

        while (elapsedTime < riseDuration)
        {
            elapsedTime += Time.fixedDeltaTime;

            float riseProgress = Mathf.SmoothStep(0f, 1f, elapsedTime / riseDuration);

            rb.MovePosition(Vector3.Lerp(impactPosition, floatPosition, riseProgress));

            yield return new WaitForFixedUpdate();
        }
    }

    // El vaivén en la superficie. Un seno completo es exactamente "sube y baja", así
    // que con bobCycles = 2 salen las dos subidas y las dos bajadas pedidas, y además
    // empieza y termina en la línea de flotación sin ningún salto de posición.
    private IEnumerator FloatOnSurface(Vector3 floatPosition)
    {
        float elapsedTime = 0f;

        while (elapsedTime < bobDuration)
        {
            elapsedTime += Time.fixedDeltaTime;

            float bobProgress = elapsedTime / bobDuration;
            float bobOffset = Mathf.Sin(bobProgress * bobCycles * 2f * Mathf.PI) * bobAmplitude;

            rb.MovePosition(new Vector3(floatPosition.x, floatPosition.y + bobOffset, floatPosition.z));

            yield return new WaitForFixedUpdate();
        }
    }

    // El descenso final. La curva es cuadrática y no lineal para que arranque despacio
    // y vaya cogiendo velocidad: bajar a ritmo constante parece un ascensor, no alguien
    // que se rinde y se va al fondo.
    private IEnumerator SinkUnderwater(Vector3 floatPosition)
    {
        float elapsedTime = 0f;

        while (elapsedTime < sinkDuration)
        {
            elapsedTime += Time.fixedDeltaTime;

            float sinkProgress = Mathf.Clamp01(elapsedTime / sinkDuration);
            float sinkOffset = sinkDepth * sinkProgress * sinkProgress;

            rb.MovePosition(new Vector3(floatPosition.x, floatPosition.y - sinkOffset, floatPosition.z));

            yield return new WaitForFixedUpdate();
        }
    }

    // Encara la cámara para que la animación de ahogarse se vea de frente y no de
    // perfil, que es como queda si Bravard muere mirando hacia donde corría.
    private IEnumerator TurnToCamera()
    {
        Camera targetCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();

        if (targetCamera == null)
        {
            yield break;
        }

        // Solo el giro horizontal: la cámara está más alta que el agua, así que mirarla
        // de verdad inclinaría a Bravard hacia atrás en lugar de girarlo sobre sí mismo.
        Vector3 directionToCamera = targetCamera.transform.position - rb.position;
        directionToCamera.y = 0f;

        // Con la cámara justo encima no habría ninguna dirección horizontal que mirar,
        // y LookRotation de un vector nulo devuelve basura
        if (directionToCamera.sqrMagnitude < 0.0001f)
        {
            yield break;
        }

        Quaternion startRotation = rb.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(directionToCamera.normalized);

        float elapsedTime = 0f;

        while (elapsedTime < turnToCameraDuration)
        {
            elapsedTime += Time.fixedDeltaTime;

            float turnProgress = Mathf.SmoothStep(0f, 1f, elapsedTime / turnToCameraDuration);

            rb.MoveRotation(Quaternion.Slerp(startRotation, targetRotation, turnProgress));

            yield return new WaitForFixedUpdate();
        }

        // El bucle puede acabar una fracción antes de completar el giro, y quedarse a
        // medio grado de frente se nota durante los segundos que aún flota
        rb.MoveRotation(targetRotation);
    }
}
