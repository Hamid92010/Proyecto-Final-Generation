using UnityEngine;
using UnityEngine.Rendering;

// Abre un agujero circular en el sprite de las rejas para que el personaje se distinga
// del fondo. El agujero lo dibuja el shader SG_BG_Texture (nodos Distance -> Step ->
// Alpha); este script solo le dice DONDE está el centro y, si se quiere, cuánto mide.
//
// El centro NO es la posicion del jugador tal cual. La camara es perspectiva y el plano
// de las rejas esta a otra profundidad que el personaje, asi que el punto del plano que
// la camara ve "encima" del jugador es la interseccion del rayo camara->jugador con ese
// plano. Mandar la posicion cruda hace que el agujero se despegue del personaje en
// cuanto este se aleja del centro de la pantalla.
//
// De regalo, esa proyeccion deja el centro EXACTAMENTE sobre el plano del sprite. Como
// el nodo Distance del grafo trabaja en 3D, si el centro estuviera a otra Z esa
// diferencia se sumaria a la distancia y el agujero se encogeria o no apareceria nunca.
[DisallowMultipleComponent]
public class MathematicalMask : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Renderer del sprite que se va a perforar (las rejas). Vacio = el Renderer de este mismo GameObject.")]
    [SerializeField] private Renderer maskedRenderer;

    [Tooltip("Transform del personaje alrededor del cual se abre el agujero.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("Camara desde la que se ve la escena. Vacio = Camera.main.")]
    [SerializeField] private Camera viewCamera;

    [Header("Ajustes")]
    [Tooltip("Proyecta al jugador sobre el plano del sprite siguiendo la vista de la camara. Desactivarlo solo tiene sentido si el sprite esta exactamente a la misma Z que el personaje.")]
    [SerializeField] private bool compensateParallax = true;

    [Tooltip("Radio del agujero en unidades de mundo MEDIDAS SOBRE EL PLANO DE LAS REJAS. Dejalo en negativo para respetar el valor que ya tenga el material.")]
    [SerializeField] private float maskRadius = -1f;

    // Referencias hasheadas a las propiedades del shader. Tienen que coincidir con el
    // campo "Reference" del Blackboard de SG_BG_Texture, que lleva guion bajo delante.
    private static readonly int playerPositionID = Shader.PropertyToID("_Player_Position");
    private static readonly int maskRadiusID = Shader.PropertyToID("_Mask_Radius");

    // Copia propia del material: asi el agujero no se escribe sobre el asset compartido
    // ni lo hereda el fondo de madera, que usa el mismo shader.
    private Material maskedMaterial;

    void Awake()
    {
        // Autocompletado de referencias, para que el componente funcione colgado del
        // propio sprite de las rejas sin tener que configurar nada mas.
        if (maskedRenderer == null) maskedRenderer = GetComponent<Renderer>();
        if (viewCamera == null) viewCamera = Camera.main;

        // Sin estas dos referencias no hay nada que calcular ni donde escribirlo.
        if (maskedRenderer == null)
        {
            Debug.LogError($"{nameof(MathematicalMask)}: falta el Renderer de las rejas.", this);
            enabled = false;
            return;
        }
        if (playerTransform == null)
        {
            Debug.LogError($"{nameof(MathematicalMask)}: falta el Transform del jugador.", this);
            enabled = false;
            return;
        }

        maskedMaterial = maskedRenderer.material;

        // Aviso temprano del error mas silencioso de todos: si el nombre no coincide con
        // el Reference del shader, SetVector no lanza excepcion, simplemente no hace
        // nada, y el agujero se queda clavado en el origen del mundo.
        if (!maskedMaterial.HasVector(playerPositionID))
        {
            Debug.LogError($"{nameof(MathematicalMask)}: el material '{maskedMaterial.name}' no expone _Player_Position. Revisa el Reference de esa propiedad en el Shader Graph.", this);
            enabled = false;
            return;
        }

        // Sin camara no se puede proyectar: el agujero se centraria en la posicion cruda
        // del jugador y se despegaria de el en pantalla.
        if (viewCamera == null && compensateParallax)
        {
            Debug.LogWarning($"{nameof(MathematicalMask)}: no hay camara asignada ni Camera.main en la escena. El agujero se centrara en la posicion cruda del jugador.", this);
        }

        // El radio solo se toca si se pidio explicitamente; si no, manda el material.
        if (maskRadius >= 0f && maskedMaterial.HasFloat(maskRadiusID))
        {
            maskedMaterial.SetFloat(maskRadiusID, maskRadius);
        }
    }

    void OnEnable()
    {
        // Actualizar justo antes de dibujar, y no en LateUpdate, quita el fotograma de
        // retraso: cuando salta este evento ya se movieron el jugador Y la camara.
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    void OnDestroy()
    {
        // Renderer.material devuelve una copia; hay que destruirla a mano.
        if (maskedMaterial != null) Destroy(maskedMaterial);
    }

    // La escena tambien se dibuja para la vista de Scene y para las previews del editor;
    // solo interesa recalcular para la camara del juego. Si no hay camara asignada no
    // hay con que comparar, asi que se recalcula para todas.
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        if (viewCamera != null && renderingCamera != viewCamera) return;

        maskedMaterial.SetVector(playerPositionID, CalculateMaskCenter());
    }

    // Devuelve el punto del plano de las rejas que la camara ve justo encima del jugador.
    private Vector3 CalculateMaskCenter()
    {
        Vector3 playerPosition = playerTransform.position;

        if (!compensateParallax || viewCamera == null) return playerPosition;

        // Plano que contiene al sprite: pasa por su pivote y mira hacia donde mira el.
        Transform maskedTransform = maskedRenderer.transform;
        Plane maskedPlane = new Plane(maskedTransform.forward, maskedTransform.position);

        // En perspectiva la linea de vista sale de la camara y pasa por el jugador; en
        // ortografica todas las lineas de vista son paralelas al eje de la camara.
        Vector3 rayOrigin = viewCamera.orthographic ? playerPosition : viewCamera.transform.position;
        Vector3 rayDirection = viewCamera.orthographic
            ? viewCamera.transform.forward
            : playerPosition - viewCamera.transform.position;

        // Si la linea es paralela al plano no hay interseccion utilizable: mas vale
        // devolver la posicion cruda que dejar el agujero en un sitio absurdo.
        if (!TryIntersectPlane(maskedPlane, rayOrigin, rayDirection, out Vector3 maskCenter))
        {
            return playerPosition;
        }

        return maskCenter;
    }

    // Interseca una RECTA (no un rayo) con el plano, para que funcione tenga el plano
    // delante o detras del origen.
    private static bool TryIntersectPlane(Plane plane, Vector3 origin, Vector3 direction, out Vector3 point)
    {
        float denominator = Vector3.Dot(plane.normal, direction);

        // Denominador ~0 significa recta paralela al plano.
        if (Mathf.Abs(denominator) < 1e-6f)
        {
            point = origin;
            return false;
        }

        // Plane guarda la ecuacion como dot(normal, p) + distance = 0.
        float distanceAlongRay = -(Vector3.Dot(plane.normal, origin) + plane.distance) / denominator;
        point = origin + direction * distanceAlongRay;
        return true;
    }
}
