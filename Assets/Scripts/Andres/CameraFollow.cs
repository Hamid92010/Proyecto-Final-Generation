using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float offsetY;

    // Quien avisa de que la partida terminó
    private GameManager gameManager;

    // Mientras siga a Bravard, cualquier movimiento suyo en Y es invisible: los dos se
    // desplazan lo mismo y en pantalla nada cambia. Al ahogarse eso arruinaba el efecto
    // (el personaje parecía quieto y el mundo entero era el que subía y bajaba), así que
    // al perder la cámara se planta y deja que se le vea flotar y hundirse.
    private bool canFollowPlayer = true;

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
        if (!canFollowPlayer)
        {
            return;
        }

        transform.position = new Vector3( transform.position.x, player.position.y + offsetY,transform.position.z);
    }

    private void StopFollowing()
    {
        canFollowPlayer = false;
    }
}
