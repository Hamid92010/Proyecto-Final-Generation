using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float offsetY;

    // Quien avisa de que la partida terminó
    private GameManager gameManager;

    // Quien pide sacudidas al recibir un golpe. Puede no estar: la cámara sigue
    // funcionando igual sin él, solo que quieta.
    private CameraShake cameraShake;

    // Mientras siga a Bravard, cualquier movimiento suyo en Y es invisible: los dos se
    // desplazan lo mismo y en pantalla nada cambia. Al ahogarse eso arruinaba el efecto
    // (el personaje parecía quieto y el mundo entero era el que subía y bajaba), así que
    // al perder la cámara se planta y deja que se le vea flotar y hundirse.
    private bool canFollowPlayer = true;

    // Posición "limpia" de la cámara, sin la sacudida encima. Se guarda aparte porque
    // el transform de verdad lleva el desvío sumado: si se leyera de ahí, cada frame
    // partiría del sitio desplazado y el temblor se acumularía en lugar de volver.
    private float basePositionX;
    private float basePositionY;
    private float basePositionZ;

    // --- Enfoque guionizado -------------------------------------------------
    // Sirve para señalar algo al jugador (hoy, la jaula al abrirse). No sustituye al
    // seguimiento: lo mezcla. El peso va de 0 (jugador) a 1 (objetivo) y son las
    // rampas de ese peso las que hacen el viaje de ida y el de vuelta.

    // A dónde mira la cámara mientras dura el enfoque
    private Transform focusTarget;
    private Vector3 focusOffset;

    // 0 = sobre el jugador, 1 = sobre el objetivo
    private float focusWeight;

    // El viaje en curso. Uno nuevo cancela el anterior en vez de sumarse a él.
    private Coroutine focusRoutine;

    // Hay un enfoque activo o a medio camino
    public bool IsFocusing => focusWeight > 0f || focusRoutine != null;

    private void Awake()
    {
        basePositionX = transform.position.x;
        basePositionY = transform.position.y;
        basePositionZ = transform.position.z;

        // Vive en este mismo objeto; que falte es perfectamente válido
        cameraShake = GetComponent<CameraShake>();
    }

    private void OnEnable()
    {
        // Mismo patrón de búsqueda que sigue el resto del proyecto
        gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.OnGameOver += StopFollowing;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameOver -= StopFollowing;
        }
    }

    private void LateUpdate()
    {
        // Al perder, la altura se congela donde estuviera; la sacudida se sigue
        // aplicando para que una que estuviese a medias termine de apagarse sola
        if (canFollowPlayer && player != null)
        {
            basePositionY = player.position.y + offsetY;
        }

        Vector3 followPosition = new Vector3(basePositionX, basePositionY, basePositionZ);

        // El enfoque se mezcla con el seguimiento en lugar de sustituirlo. Así el
        // viaje de vuelta no necesita saber dónde estaba el jugador al empezar: si se
        // ha movido, la cámara aterriza donde está AHORA y no donde estaba entonces.
        Vector3 targetPosition = followPosition;

        if (focusTarget != null && focusWeight > 0f)
        {
            Vector3 focusPosition = focusTarget.position + focusOffset;

            // La Z se queda en la de reposo. Viajar tambien en profundidad acerca la
            // camara al objeto, y eso no se lee como un desplazamiento sino como un
            // zoom: el encuadre entero cambia de escala en vez de moverse.
            focusPosition.z = basePositionZ;

            targetPosition = Vector3.Lerp(followPosition, focusPosition, focusWeight);
        }

        Vector3 shakeOffset = cameraShake != null ? cameraShake.CurrentOffset : Vector3.zero;

        // Único punto del proyecto que escribe la posición de la cámara. Todo lo que
        // quiera moverla pasa por aquí en forma de desvío, y así nada se queda pegado.
        transform.position = targetPosition + shakeOffset;
    }

    private void StopFollowing()
    {
        canFollowPlayer = false;
    }

    // Lleva la cámara hasta un objetivo y la deja ahí. La vuelta se pide aparte con
    // EndFocus: quien orquesta la escena decide cuánto tiempo se queda mirando.
    public void StartFocus(Transform target, Vector3 offset, float travelDuration)
    {
        if (target == null)
        {
            return;
        }

        focusTarget = target;
        focusOffset = offset;

        RampFocusTo(1f, travelDuration);
    }

    // Devuelve la cámara al jugador
    public void EndFocus(float returnDuration)
    {
        RampFocusTo(0f, returnDuration);
    }

    private void RampFocusTo(float targetWeight, float duration)
    {
        // Un enfoque nuevo cancela el que hubiera a medias. Dos rampas vivas se
        // pelearían por focusWeight y la cámara temblaría entre los dos destinos.
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
        }

        focusRoutine = StartCoroutine(RampFocusRoutine(targetWeight, duration));
    }

    private System.Collections.IEnumerator RampFocusRoutine(float targetWeight, float duration)
    {
        float startWeight = focusWeight;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // SmoothStep para que la cámara arranque y frene suave. Un desplazamiento
            // lineal se para en seco al llegar y delata que es un movimiento calculado.
            focusWeight = Mathf.Lerp(startWeight, targetWeight, Mathf.SmoothStep(0f, 1f, elapsedTime / duration));

            yield return null;
        }

        focusWeight = targetWeight;

        // De vuelta en el jugador ya no hace falta seguir apuntando a nada
        if (targetWeight <= 0f)
        {
            focusTarget = null;
        }

        focusRoutine = null;
    }
}
