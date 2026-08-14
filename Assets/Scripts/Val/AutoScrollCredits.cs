using UnityEngine;

public class AutoScrollCredits : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float scrollSpeed = 50f; 
    [SerializeField] private float resetPositionY = 1000f; 

    private RectTransform rectTransform;
    private Vector2 initialPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
       
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = initialPosition;
        }
    }

    private void Update()
    {
        
        rectTransform.anchoredPosition += Vector2.up * (scrollSpeed * Time.deltaTime);

        
        if (rectTransform.anchoredPosition.y >= resetPositionY)
        {
            
            enabled = false;
        }
    }
}