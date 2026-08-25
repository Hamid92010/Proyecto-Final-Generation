using UnityEngine;
using TMPro;

public class TutorialTrigger : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Configuración del Mensaje")]
    [TextArea(2, 4)]
    [SerializeField] private string message = "¡Sube rápido por las plataformas para escapar del agua!";
    [SerializeField] private float displayDuration = 3f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (tutorialPanel != null && instructionText != null)
            {
                instructionText.text = message;
                tutorialPanel.SetActive(true);
                CancelInvoke(nameof(HidePanel));
                Invoke(nameof(HidePanel), displayDuration);
            }

            
            gameObject.SetActive(false);
        }
    }

    private void HidePanel()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }
}