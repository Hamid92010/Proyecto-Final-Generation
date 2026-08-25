using UnityEngine;

// ---------------------------------------------------------------------------
// Puente entre el desenlace de la partida y el Animator_Chicks.
//
// Es el hermano pequeno de PlayerAnimationBridge y sigue el mismo principio: no
// decide nada, solo traduce eventos del GameManager a parametros del Animator.
//
// La diferencia es que aqui no hay fisica que leer. Los pollitos estan
// encerrados: lo unico que les ocurre es que la partida se gane o se acabe el
// tiempo, asi que todo llega por suscripcion y no hace falta Update.
// ---------------------------------------------------------------------------
[RequireComponent(typeof(Animator))]
public class ChicksAnimationBridge : MonoBehaviour
{
    // Animator de los pollitos (Animator_Chicks)
    private Animator animator;

    // Fuente de verdad del desenlace de la partida
    private GameManager gameManager;

    // Los dos parametros del Animator_Chicks. Ambos son TRIGGERS.
    private readonly int hashIsWinning = Animator.StringToHash("IsWinning");
    private readonly int hashIsBeingDefeated = Animator.StringToHash("IsBeingDefeated");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        // Mismo patron de busqueda que el resto del proyecto
        gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.OnGameFinished += TriggerVictory;
            gameManager.OnGameOverCaused += TriggerDefeat;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameFinished -= TriggerVictory;
            gameManager.OnGameOverCaused -= TriggerDefeat;
        }
    }

    // Celebracion al ser liberados. A diferencia de Bravard, aqui no hay ningun
    // bool que acompanie al trigger: la transicion sale de AnyState sin mas.
    private void TriggerVictory()
    {
        animator.SetTrigger(hashIsWinning);
    }

    // Derrota. El Animator_Chicks solo tiene animacion para el final por tiempo
    // (Chicks_DefeatedByTime): ahogarse no les afecta porque la jaula esta en alto,
    // asi que la derrota por agua se ignora a proposito.
    private void TriggerDefeat(GameOverCause cause)
    {
        if (cause != GameOverCause.Time)
        {
            return;
        }

        animator.SetTrigger(hashIsBeingDefeated);
    }
}
