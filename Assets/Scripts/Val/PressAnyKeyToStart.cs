using UnityEngine;
using UnityEngine.InputSystem;

public class PressAnyKeyToStart : MonoBehaviour
{
    [Header("Referencias de Paneles")]
    [SerializeField] private GameObject gameTitleView;
    [SerializeField] private GameObject startView;

    private bool hasPressedKey = false;

    private void Update()
    {
        
        if (!hasPressedKey && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame || 
            !hasPressedKey && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            hasPressedKey = true;
            ShowMainMenuButtons();
        }
    }

    private void ShowMainMenuButtons()
    {
        if (gameTitleView != null) gameTitleView.SetActive(false);
        if (startView != null) startView.SetActive(true);
    }
}