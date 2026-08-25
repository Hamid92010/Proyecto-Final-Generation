using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistoryViewController : MonoBehaviour
{
    [System.Serializable]
    public class HistoryPage
    {
        [Tooltip("Imagen que aparecerá en el lado izquierdo.")]
        public Sprite image;

        [TextArea(4, 10)]
        [Tooltip("Texto correspondiente a esta imagen.")]
        public string text;
    }

    [Header("Contenido de la historia")]
    [SerializeField]
    private List<HistoryPage> pages =
        new List<HistoryPage>();

    [Header("Referencias de interfaz")]
    [SerializeField] private Image storyImage;
    [SerializeField] private CanvasGroup imageCanvasGroup;
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;

    [Header("Paneles")]
    [SerializeField] private GameObject historyView;
    [SerializeField] private GameObject startView;

    [Header("Configuración de imagen")]
    [SerializeField] private float imageFadeDuration = 3f;

    [Header("Configuración del texto")]
    [SerializeField] private float charactersPerSecond = 30f;

    private int currentPageIndex;

    private bool isTyping;
    private bool isChangingPage;
    private bool hasStarted;

    private Coroutine pageTransitionCoroutine;
    private Coroutine typewriterCoroutine;

    private void Awake()
    {
        nextButton.onClick.AddListener(HandleNextButton);
        skipButton.onClick.AddListener(SkipHistory);
    }

    private void OnEnable()
    {
        // Evitar comenzar antes de que todas las referencias
        // hayan sido inicializadas.
        if (!hasStarted)
        {
            hasStarted = true;
            StartHistory();
        }
    }

    private void OnDestroy()
    {
        nextButton.onClick.RemoveListener(HandleNextButton);
        skipButton.onClick.RemoveListener(SkipHistory);
    }

    public void StartHistory()
    {
        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning(
                "HistoryViewController no tiene páginas configuradas."
            );

            FinishHistory();
            return;
        }

        StopAllCoroutines();

        currentPageIndex = 0;
        isTyping = false;
        isChangingPage = false;

        storyText.text = string.Empty;
        storyText.maxVisibleCharacters = 0;

        imageCanvasGroup.alpha = 0f;

        nextButton.interactable = false;
        skipButton.interactable = true;

        pageTransitionCoroutine = StartCoroutine(
            ShowFirstPage()
        );
    }

    private IEnumerator ShowFirstPage()
    {
        isChangingPage = true;

        HistoryPage firstPage = pages[currentPageIndex];

        storyImage.sprite = firstPage.image;
        storyText.text = string.Empty;

        // La primera imagen aparece progresivamente.
        yield return FadeImage(0f, 1f);

        isChangingPage = false;
        nextButton.interactable = true;

        // Cuando termina el fade, comienza la máquina de escribir.
        StartTypewriter(firstPage.text);
    }

    private void HandleNextButton()
    {
        // Durante el cambio de imagen ignoramos el botón Siguiente.
        if (isChangingPage)
        {
            return;
        }

        // Si el texto todavía se está escribiendo,
        // el primer clic lo muestra completamente.
        if (isTyping)
        {
            CompleteCurrentText();
            return;
        }

        // Si ya estamos en la última página, termina la historia.
        if (currentPageIndex >= pages.Count - 1)
        {
            FinishHistory();
            return;
        }

        // Si el texto ya está completo, avanzar a otra página.
        currentPageIndex++;

        pageTransitionCoroutine = StartCoroutine(
            ChangePage()
        );
    }

    private IEnumerator ChangePage()
    {
        isChangingPage = true;
        nextButton.interactable = false;

        // El texto anterior desaparece inmediatamente.
        storyText.text = string.Empty;
        storyText.maxVisibleCharacters = 0;

        // La imagen actual desaparece progresivamente.
        yield return FadeImage(1f, 0f);

        // Cambiar la imagen cuando está completamente transparente.
        HistoryPage newPage = pages[currentPageIndex];
        storyImage.sprite = newPage.image;

        // La imagen nueva aparece progresivamente.
        yield return FadeImage(0f, 1f);

        isChangingPage = false;
        nextButton.interactable = true;

        // Solamente después comienza el texto nuevo.
        StartTypewriter(newPage.text);
    }

    private IEnumerator FadeImage(
        float initialAlpha,
        float finalAlpha
    )
    {
        float elapsedTime = 0f;
        imageCanvasGroup.alpha = initialAlpha;

        while (elapsedTime < imageFadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / imageFadeDuration
            );

            imageCanvasGroup.alpha = Mathf.Lerp(
                initialAlpha,
                finalAlpha,
                progress
            );

            yield return null;
        }

        imageCanvasGroup.alpha = finalAlpha;
    }

    private void StartTypewriter(string completeText)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        typewriterCoroutine = StartCoroutine(
            TypeText(completeText)
        );
    }

    private IEnumerator TypeText(string completeText)
    {
        isTyping = true;

        // Guardamos el texto completo, pero inicialmente
        // no mostramos ningún carácter.
        storyText.text = completeText;
        storyText.maxVisibleCharacters = 0;

        // Obliga a TextMeshPro a calcular los caracteres.
        storyText.ForceMeshUpdate();

        int totalCharacters =
            storyText.textInfo.characterCount;

        float visibleCharacters = 0f;

        while (
            storyText.maxVisibleCharacters <
            totalCharacters
        )
        {
            visibleCharacters +=
                charactersPerSecond *
                Time.unscaledDeltaTime;

            storyText.maxVisibleCharacters =
                Mathf.Min(
                    Mathf.FloorToInt(visibleCharacters),
                    totalCharacters
                );

            yield return null;
        }

        storyText.maxVisibleCharacters =
            totalCharacters;

        isTyping = false;
        typewriterCoroutine = null;
    }

    private void CompleteCurrentText()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        storyText.maxVisibleCharacters =
            int.MaxValue;

        isTyping = false;
    }

    public void SkipHistory()
    {
        Debug.Log("El jugador omitió la historia.");

        StopAllCoroutines();

        isTyping = false;
        isChangingPage = false;

        FinishHistory();
    }

    private void FinishHistory()
    {
        if (startView != null)
        {
            startView.SetActive(true);
        }

        if (historyView != null)
        {
            historyView.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
