using System.Collections;
using UnityEngine;

// ---------------------------------------------------------------------------
// Sacudida de la cámara al recibir un golpe.
//
// Este componente NO toca el transform: solo publica un desplazamiento que
// CameraFollow suma a la posición que ya calcula. Es a propósito. Si los dos
// escribieran la posición se pisarían, y como CameraFollow conserva la X y la Z
// que encuentra, el desvío de la sacudida se le quedaría pegado para siempre:
// la cámara acabaría desplazada un poco más con cada golpe.
//
// Va en el mismo objeto que CameraFollow (la Main Camera).
// ---------------------------------------------------------------------------
public class CameraShake : MonoBehaviour
{
    [Header("Sacudida")]
    [Tooltip("Segundos que dura la sacudida de un golpe.")]
    [SerializeField, Min(0f)] private float shakeDuration = 0.25f;

    [Tooltip("Desvío máximo de la cámara, en unidades. Es el golpe inicial: a partir de ahí baja hasta cero.")]
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.28f;

    [Tooltip("Con qué rapidez cambia de dirección el temblor. Más alto = más nervioso; más bajo = un bamboleo largo.")]
    [SerializeField, Min(0.1f)] private float shakeFrequency = 25f;

    // Lo que CameraFollow le suma a la posición de la cámara este frame
    public Vector3 CurrentOffset => currentOffset;
    private Vector3 currentOffset;

    // Quien avisa de los golpes. Es el mismo evento que dispara la animación de
    // dolor de Bravard, así que el temblor y el gesto entran a la vez.
    private PlayerCollisions playerCollisions;

    // Para no seguir sacudiendo con la partida ya decidida
    private GameManager gameManager;

    // La sacudida en curso, si la hay. Un golpe nuevo la reinicia en vez de sumarse.
    private Coroutine shakeRoutine;

    // Desplazamientos de la textura de ruido. Sin ellos las dos coordenadas leerían
    // la MISMA onda y la cámara temblaría en diagonal perfecta, no de forma errática.
    private float noiseSeedX;
    private float noiseSeedY;

    private void Awake()
    {
        noiseSeedX = Random.value * 100f;
        noiseSeedY = Random.value * 100f + 100f;
    }

    private void OnEnable()
    {
        // Mismo patrón de búsqueda que sigue el resto del proyecto
        playerCollisions = FindAnyObjectByType<PlayerCollisions>();
        gameManager = FindAnyObjectByType<GameManager>();

        if (playerCollisions != null)
        {
            playerCollisions.HitByBall += Shake;
        }
    }

    private void OnDisable()
    {
        if (playerCollisions != null)
        {
            playerCollisions.HitByBall -= Shake;
        }
    }

    // Público para poder sacudir desde cualquier otro impacto que se añada más
    // adelante sin tener que pasar por el evento de las pelotas.
    public void Shake()
    {
        // Ganada o perdida la partida, el jugador ya no controla nada: una roca que
        // llegue tarde no debe zarandear la cámara sobre la pose final ni sobre el
        // ahogamiento. Es la misma condición que usa el puente de animación.
        if (gameManager != null && (gameManager.gameOver || gameManager.gameFinished))
        {
            return;
        }

        // Un segundo golpe reinicia la sacudida a plena potencia. Encadenar dos
        // corrutinas las haría competir por currentOffset y el temblor se anularía
        // a sí mismo justo cuando más fuerte debería verse.
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;

            // El desvío se apaga de forma lineal hasta cero, así que la cámara termina
            // exactamente donde estaba y no hace falta recolocarla al acabar
            float decay = 1f - (elapsedTime / shakeDuration);

            // Ruido Perlin en vez de valores al azar cada frame: el azar puro salta de
            // un extremo a otro y parpadea, mientras que el ruido recorre las posiciones
            // intermedias y se lee como una sacudida de verdad.
            float noiseTime = Time.time * shakeFrequency;
            float horizontalNoise = Mathf.PerlinNoise(noiseSeedX, noiseTime) * 2f - 1f;
            float verticalNoise = Mathf.PerlinNoise(noiseSeedY, noiseTime) * 2f - 1f;

            // La Z se queda quieta: acercar y alejar la cámara en un juego de perfil
            // se ve como un tirón de zoom, no como un golpe.
            currentOffset = new Vector3(horizontalNoise, verticalNoise, 0f) * (shakeAmplitude * decay);

            yield return null;
        }

        currentOffset = Vector3.zero;
        shakeRoutine = null;
    }
}
