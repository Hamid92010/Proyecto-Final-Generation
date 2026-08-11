using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateTimerText(float timeToFinishGame)
    {
        int minutes = Mathf.FloorToInt(timeToFinishGame / 60);
        int seconds = Mathf.FloorToInt(timeToFinishGame % 60);
        timerText.text = $"{minutes:0}:{seconds:00}";
    }
}
