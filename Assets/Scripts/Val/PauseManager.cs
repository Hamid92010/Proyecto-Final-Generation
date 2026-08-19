using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseManager : MonoBehaviour
{
    [Header("Referencia del Texto/Panel de Pausa")]
    [SerializeField] private GameObject pauseMenuPanel;

    private bool isPaused = false;

    private void Start()
    {
      
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        Time.timeScale = 1f;
    }

    private void Update()
    {
        bool pPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            pPressed = true;
        }
#else
        if (Input.GetKeyDown(KeyCode.P))
        {
            pPressed = true;
        }
#endif

        if (pPressed)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isPaused);
        }

        
        Time.timeScale = isPaused ? 0f : 1f;
    }
}