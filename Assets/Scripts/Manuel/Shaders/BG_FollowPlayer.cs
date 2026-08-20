using UnityEngine;

// Pega el sprite de fondo a la altura del jugador. El sprite no lleva la textura
// pintada en sus UV: SG_BG_Texture calcula las UV a partir de la POSICION DE MUNDO del
// pixel, asi que al mover el quad la textura se queda quieta en el mundo y lo que se ve
// es un fondo infinito que se desplaza. Por eso este script y el tiling del shader son
// la misma pieza: si el quad deja de moverse, el fondo deja de scrollear.
public class BG_FollowPlayer : MonoBehaviour
{
    //Declare variables
    [SerializeField] Transform playerTransform;
    [SerializeField] float offsetX = 0f, offsetY = 0f, offsetZ = 0f;
    private Vector3 playerYPosition = new Vector3(0, 0, 0);

    void Awake()
    {
        // Sin jugador este componente petaria en cada frame; mejor apagarlo y avisar.
        if (playerTransform == null)
        {
            Debug.LogError($"{nameof(BG_FollowPlayer)}: falta el Transform del jugador.", this);
            enabled = false;
        }
    }

    // Late para leer la posicion del jugador ya resuelta por la fisica de este frame.
    void LateUpdate()
    {
        // Solo se copia la altura: X y Z quedan clavadas en el offset porque el quad es
        // lo bastante ancho como para cubrir todo el recorrido horizontal del nivel.
        playerYPosition.y = playerTransform.position.y;
        transform.position = new Vector3(offsetX, playerYPosition.y + offsetY, offsetZ);
    }
}
