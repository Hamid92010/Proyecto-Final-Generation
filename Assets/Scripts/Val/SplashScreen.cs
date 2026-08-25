using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    [Header("Logo")]
    [SerializeField] private CanvasGroup studioLogo;
    [SerializeField, Min(0.01f)]
    private float logoFadeDuration = 10f;

    [Header("Pantalla negra")]
    [SerializeField] private CanvasGroup blackFade;

    [SerializeField, Min(0.01f)]
    private float blackFadeDuration = 5f;

    [SerializeField, Min(0f)]
    private float blackScreenTime = 1f;

    [Header("Cortinas")]
    [SerializeField] private Animator curtainsAnimator;
    [SerializeField] private string closeTriggerName = "Close";

    [Header("Siguiente escena")]
    [SerializeField] private string nextSceneName = "01_MainMenu";

    private IEnumerator Start()
    {
        // Estado inicial:
        // logo visible y pantalla negra transparente.
        studioLogo.alpha = 1f;
        blackFade.alpha = 0f;

        // 1. Las cortinas comienzan a cerrarse.
        if (curtainsAnimator != null)
        {
            curtainsAnimator.SetTrigger(closeTriggerName);
        }

        // 2. El logo se desvanece durante el tiempo establecido.
        yield return StartCoroutine(FadeOutLogo());

        // 3. Cuando el logo desapareció, comienza BlackFade.
        yield return StartCoroutine(FadeToBlack());

        // 4. Asegurar que la pantalla esté completamente negra.
        blackFade.alpha = 1f;

        // Permitir que Unity renderice el negro completo.
        yield return new WaitForEndOfFrame();

        // 5. Mantener la pantalla negra durante un segundo.
        yield return new WaitForSecondsRealtime(blackScreenTime);

        // 6. Cambiar de escena.
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator FadeOutLogo()
    {
        float elapsedTime = 0f;

        while (elapsedTime < logoFadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / logoFadeDuration
            );

            // Cambio lineal y claramente progresivo de 1 a 0.
            studioLogo.alpha = Mathf.Lerp(
                1f,
                0f,
                progress
            );

            yield return null;
        }

        studioLogo.alpha = 0f;
    }

    private IEnumerator FadeToBlack()
    {
        float elapsedTime = 0f;

        // BlackFade empieza completamente transparente.
        blackFade.alpha = 0f;

        while (elapsedTime < blackFadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / blackFadeDuration
            );

            // Cambio lineal y progresivo de transparente a negro.
            blackFade.alpha = Mathf.Lerp(
                0f,
                1f,
                progress
            );

            yield return null;
        }

        blackFade.alpha = 1f;
    }
}