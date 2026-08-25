using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void LoadGameplayScene()
    {
        SDKAnalytics.Instance.TrackEvent("start_button_click");
        GameplayAnalytics.Instance?.GameRestarted();
        SceneManager.LoadScene("02_GamePlay");
    }

    public void LoadMainMenuScene()
    {
        GameplayAnalytics.Instance?.GameQuitEarly();
        SceneManager.LoadScene("01_MainMenu");
    }
}