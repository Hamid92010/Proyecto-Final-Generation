using UnityEngine;
using System;

public class PlayerCollisions : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public event Action TouchObstacle;
    public event Action UnTouchObstacle;
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

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            TouchObstacle?.Invoke();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            UnTouchObstacle?.Invoke();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Water"))
        {
            if (gameManager != null)
            {
                gameManager.TriggerGameOver();
            }

        }
    }

}
