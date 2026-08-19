using System;
using System.Collections;
using UnityEngine;

// Controla los golpes que recibe el jugador (ej. de FallingRock, que llama a
// OnRockImpact vía SendMessage). Al ser golpeado pierde momento, queda
// invencible y parpadeando un tiempo, y tras "maxHits" golpes pierde la partida.
[RequireComponent(typeof(Rigidbody))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHits = 3;
    [SerializeField, Min(0.1f)] private float invincibilityDuration = 3f;
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.1f;
    [SerializeField] private PlayerController_02 playerController;

    public event Action<int, int> OnHit; // golpes actuales, golpes máximos
    public event Action OnDefeated;

    private Rigidbody rb;
    private Renderer[] renderers;
    private int currentHits;
    private bool isInvincible;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<Renderer>();

        if (playerController == null)
            playerController = GetComponent<PlayerController_02>();
    }

    public void OnRockImpact()
    {
        if (isInvincible) return;

        currentHits++;
        OnHit?.Invoke(currentHits, maxHits);

        // Pierde el momento que llevaba (solo movimiento horizontal, conserva la caída).
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        if (playerController != null)
            playerController.Stun(invincibilityDuration);

        if (currentHits >= maxHits)
        {
            Defeat();
            return;
        }

        StartCoroutine(InvincibilityBlink());
    }

    private void Defeat()
    {
        OnDefeated?.Invoke();
        Debug.Log("GAME OVER: el jugador perdió la partida");

        if (playerController != null)
            playerController.enabled = false;
    }

    private IEnumerator InvincibilityBlink()
    {
        isInvincible = true;
        float elapsed = 0f;
        bool isVisible = true;

        while (elapsed < invincibilityDuration)
        {
            isVisible = !isVisible;
            SetRenderersVisible(isVisible);
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        SetRenderersVisible(true);
        isInvincible = false;
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (Renderer r in renderers)
            r.enabled = visible;
    }
}
