using UnityEngine;

public class GameManager : MonoBehaviour
{

    [field: SerializeField]
    public float timeToFinishGame { get; private set; } = 90f;
    public bool isGameStarted = false;
    public bool gameOver = false;
    public bool gameFinished = false;
    public bool isGamePaused = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
