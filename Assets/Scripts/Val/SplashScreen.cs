using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    [SerializeField] private float waitTime = 3f;
    [SerializeField] private string nextSceneName = "01_MainMenu";

    private void Start()
    {
        StartCoroutine(ChangeSceneAfterDelay());
    }

    private IEnumerator ChangeSceneAfterDelay()
    {
        
        yield return new WaitForSeconds(waitTime);
        
    
        SceneManager.LoadScene(nextSceneName);
    }
}