using UnityEngine;
using UnityEngine.UI;

public class WaterProximityUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Slider dangerSlider;

    [Header("World References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform waterTransform;

    [Header("Level Bounds")]
    [Tooltip("Posición Y del player en el fondo del nivel (Piso / Inicio)")]
    [SerializeField] private float levelMinYPlayer;
    [Tooltip("Posición Y del agua en el fondo del nivel (Piso / Inicio)")]
    [SerializeField] private float levelMinYWater = -3.4f;

    [Tooltip("Posición Y de la meta / plataforma más alta")]
    [SerializeField] private float levelMaxY;

    private RectTransform handleRect;
    private GameManager gameManager;


    private void Start()
    {
        if (dangerSlider != null && dangerSlider.handleRect != null)
        {
            handleRect = dangerSlider.handleRect;
        }

        //Se toma el valor inicial del jugador como el mínimo del nivel para que la barra de peligro se llene correctamente
        levelMinYPlayer = playerTransform.position.y;

        gameManager =FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SearchPlatformToWin();
            levelMaxY = gameManager.yPosToFinishGame;
        }
    }

    private void Update()
    {
        if (playerTransform == null || waterTransform == null || dangerSlider == null)
            return;

        float waterProgress;
        if (waterTransform.position.y < levelMinYWater)
        {
            waterProgress = 0;
        }
        else
        {
            waterProgress = Mathf.InverseLerp(levelMinYWater, levelMaxY, waterTransform.position.y);
        }
        dangerSlider.value = Mathf.Clamp01(waterProgress);

        
        if (handleRect != null)
        {
            float playerProgress = Mathf.InverseLerp(levelMinYPlayer, levelMaxY, playerTransform.position.y);
            
            
            handleRect.anchorMin = new Vector2(handleRect.anchorMin.x, playerProgress);
            handleRect.anchorMax = new Vector2(handleRect.anchorMax.x, playerProgress);
            handleRect.anchoredPosition = Vector2.zero;
        }
    }
}