using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class CanvasGroupFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public IEnumerator FadeTo(
        float targetAlpha,
        float duration,
        bool disableWhenFinished = false)
    {
        gameObject.SetActive(true);

        float initialAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
        }
        else
        {
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsedTime / duration
                );

                float smoothProgress = Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

                canvasGroup.alpha = Mathf.Lerp(
                    initialAlpha,
                    targetAlpha,
                    smoothProgress
                );

                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        bool isVisible = targetAlpha > 0.99f;

        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;

        if (disableWhenFinished && targetAlpha <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    public void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;

        bool isVisible = alpha > 0.99f;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;
    }
}