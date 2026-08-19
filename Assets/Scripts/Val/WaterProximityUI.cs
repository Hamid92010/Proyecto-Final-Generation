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
    [Tooltip("Posición Y del agua en el fondo del nivel (Piso / Inicio)")]
    [SerializeField] private float levelMinY = -4f;

    [Tooltip("Posición Y de la meta / plataforma más alta")]
    [SerializeField] private float levelMaxY = 15f;

    private RectTransform handleRect;

    private void Start()
    {
        if (dangerSlider != null && dangerSlider.handleRect != null)
        {
            handleRect = dangerSlider.handleRect;
        }
    }

    private void Update()
    {
        if (playerTransform == null || waterTransform == null || dangerSlider == null)
            return;

        
        float waterProgress = Mathf.InverseLerp(levelMinY, levelMaxY, waterTransform.position.y);
        dangerSlider.value = Mathf.Clamp01(waterProgress);

        
        if (handleRect != null)
        {
            float playerProgress = Mathf.InverseLerp(levelMinY, levelMaxY, playerTransform.position.y);
            
            
            handleRect.anchorMin = new Vector2(handleRect.anchorMin.x, playerProgress);
            handleRect.anchorMax = new Vector2(handleRect.anchorMax.x, playerProgress);
            handleRect.anchoredPosition = Vector2.zero;
        }
    }
}