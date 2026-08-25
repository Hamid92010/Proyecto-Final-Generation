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

        Vector3 shakeOffset = cameraShake != null ? cameraShake.CurrentOffset : Vector3.zero;

        // Único punto del proyecto que escribe la posición de la cámara. Todo lo que
        // quiera moverla pasa por aquí en forma de desvío, y así nada se queda pegado.
        transform.position = new Vector3(
            basePositionX + shakeOffset.x,
            basePositionY + shakeOffset.y,
            basePositionZ + shakeOffset.z);
    }

    private void StopFollowing()
    {
        canFollowPlayer = false;
    }
}
