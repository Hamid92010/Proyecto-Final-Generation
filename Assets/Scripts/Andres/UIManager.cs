using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    private TimeManager timeManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnEnable()
    {
        timeManager = FindAnyObjectByType<TimeManager>();
        if (timeManager != null)
        {
            timeManager.OnTimeChanged += UpdateTimerText;
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeChanged -= UpdateTimerText;
        }
    }
    public void UpdateTimerText(float timeToFinishGame)
    {
        int minutes = Mathf.FloorToInt(timeToFinishGame / 60);
        int seconds = Mathf.FloorToInt(timeToFinishGame % 60);
        timerText.text = $"{minutes:0}:{seconds:00}";
    }
}
