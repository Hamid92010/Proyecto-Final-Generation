using UnityEngine;
using UnityEngine.InputSystem;

public class PressAnyKeyToStart : MonoBehaviour
{
    [Header("Referencias de paneles")]
    [SerializeField] private GameObject gameTitleView;
    [SerializeField] private GameObject historyView;

    private bool hasPressedKey;

    private void Update()
    {
        if (hasPressedKey)
        {
            return;
        }

        bool keyboardPressed =
            Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame;

        bool mousePressed =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        if (keyboardPressed || mousePressed)
        {
            hasPressedKey = true;
            ShowHistory();
        }
    }

    private void ShowHistory()
    {
        if (gameTitleView != null)
        {
            gameTitleView.SetActive(false);
        }

        if (historyView != null)
        {
            historyView.SetActive(true);
        }
    }
}