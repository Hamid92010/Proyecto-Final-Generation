using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void LoadGameplayScene()
    {
        SceneManager.LoadScene("02_GamePlay");
    }

    public void LoadMainMenuScene()
    {
        SceneManager.LoadScene("01_MainMenu");
    }
}