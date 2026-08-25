using UnityEngine;
using System;

public class PlayerCollisions : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public event Action TouchObstacle;
    public event Action UnTouchObstacle;
    public event Action<bool> StateTriggerWin;
    // Golpe recibido de una pelota. Lo consume la capa de animacion para reaccionar;
    // el empujon fisico lo sigue aplicando ObstacleKnockback por su cuenta.
    public event Action HitByBall;
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
                gameManager.TriggerGameOver(GameOverCause.Water);
            }
        }

        if (collision.gameObject.CompareTag("Ball"))
        {
            // Sin AudioManager en la escena el golpe no suena, pero no rompe la colisión
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayImpactBallEffect();
            }

            HitByBall?.Invoke();
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


