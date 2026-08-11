using UnityEngine;

public class WaterBehaviour : MonoBehaviour
{
    private GameManager gameManager;
    private UIManager uiManager;
    private TimeManager timeManager;
    [SerializeField] private float waterSpeed = 0.5f;
    [SerializeField] private float initialYPos = -4f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        uiManager = FindAnyObjectByType<UIManager>();
        timeManager = FindAnyObjectByType<TimeManager>();
        transform.position = new Vector3(transform.position.x, initialYPos, transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameManager.isGameStarted || gameManager.gameOver || gameManager.gameFinished || gameManager.isGamePaused)
        {
            return;
        }

        transform.position += Vector3.up * waterSpeed * Time.deltaTime;
    }


}
