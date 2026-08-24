using UnityEngine;
using System;

public class PlayerCollisions : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public event Action TouchObstacle;
    public event Action UnTouchObstacle;
    public event Action<bool> StateTriggerWin;
    private GameManager gameManager;

    private void OnEnable()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            UnTouchObstacle?.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WinTrigger"))
        {
            StateTriggerWin?.Invoke(true);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            TouchObstacle?.Invoke();
        }

        if (collision.gameObject.CompareTag("Water"))
        {
            if (gameManager != null)
            {
                gameManager.TriggerGameOver();
            }
        }

        if (collision.gameObject.CompareTag("Ball"))
        {
            AudioManager.Instance.PlayImpactBallEffect();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WinTrigger"))
        {
            StateTriggerWin?.Invoke(false);
        }
    }
}


