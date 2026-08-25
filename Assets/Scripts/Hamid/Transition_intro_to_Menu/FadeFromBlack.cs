using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeFromBlack : MonoBehaviour
{
    [Header("Tiempos")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float fadeDuration = 3f;

    [Header("Configuración")]
    [SerializeField] private bool disableWhenFinished = true;

    private CanvasGroup blackCanvasGroup;

    private void Awake()
    {
        blackCanvasGroup = GetComponent<CanvasGroup>();

        // La escena comienza completamente cubierta.
        blackCanvasGroup.alpha = 1f;

        // Impedir interacción mientras la pantalla está negra.
        blackCanvasGroup.interactable = false;
        blackCanvasGroup.blocksRaycasts = true;
    }

    private IEnumerator Start()
    {
        // Mantener la pantalla completamente negra.
        yield return new WaitForSecondsRealtime(startDelay);

        // Quitar progresivamente la pantalla negra.
        yield return FadeOut();

        // Permitir interacción con la interfaz de la escena.
        blackCanvasGroup.blocksRaycasts = false;

        // Desactivar el objeto cuando ya no sea necesario.
        if (disableWhenFinished)
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        blackCanvasGroup.alpha = 1f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / fadeDuration
            );

            blackCanvasGroup.alpha = Mathf.Lerp(
                1f,
                0f,
                progress
            );

            yield return null;
        }

        blackCanvasGroup.alpha = 0f;
    }
}
