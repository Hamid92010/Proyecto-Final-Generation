using UnityEngine;

public class CurtainSpotlightMovement : MonoBehaviour
{
    [Header("Luces")]
    [SerializeField] private Transform leftSpotlight;
    [SerializeField] private Transform rightSpotlight;

    [Header("Centro del telón")]
    [SerializeField] private Transform curtainAimCenter;

    [Header("Área de movimiento")]
    [SerializeField] private float horizontalRange = 3f;
    [SerializeField] private float verticalRange = 1.5f;

    [Header("Movimiento")]
    [SerializeField] private float movementSpeed = 0.7f;
    [SerializeField] private float rotationSmoothness = 4f;

    [Header("Variación")]
    [SerializeField] private float verticalSpeed = 0.65f;
    [SerializeField] private float secondaryMovement = 0.2f;

    private Vector3 leftAimPosition;
    private Vector3 rightAimPosition;

    private void Update()
    {
        if (
            leftSpotlight == null ||
            rightSpotlight == null ||
            curtainAimCenter == null
        )
        {
            return;
        }

        float time =
            Time.unscaledTime * movementSpeed;

        CalculateAimPositions(time);

        RotateTowards(
            leftSpotlight,
            leftAimPosition
        );

        RotateTowards(
            rightSpotlight,
            rightAimPosition
        );
    }

    private void CalculateAimPositions(float time)
    {
        // Movimiento horizontal principal.
        float horizontalWave = Mathf.Sin(time);

        // Variación secundaria para evitar un movimiento mecánico.
        float leftVariation =
            Mathf.Sin(time * 2.1f) *
            horizontalRange *
            secondaryMovement;

        float rightVariation =
            Mathf.Sin((time * 1.8f) + 1.5f) *
            horizontalRange *
            secondaryMovement;

        float leftX =
            horizontalWave * horizontalRange +
            leftVariation;

        float rightX =
            -horizontalWave * horizontalRange +
            rightVariation;

        // Movimiento vertical independiente.
        float leftY =
            Mathf.Sin(
                time * verticalSpeed + 0.5f
            ) * verticalRange;

        float rightY =
            Mathf.Sin(
                time * verticalSpeed + 2.5f
            ) * verticalRange;

        // Los puntos se calculan con los ejes locales
        // de CurtainAimCenter.
        leftAimPosition =
            curtainAimCenter.position +
            curtainAimCenter.right * leftX +
            curtainAimCenter.up * leftY;

        rightAimPosition =
            curtainAimCenter.position +
            curtainAimCenter.right * rightX +
            curtainAimCenter.up * rightY;
    }

    private void RotateTowards(
        Transform spotlight,
        Vector3 targetPosition
    )
    {
        Vector3 direction =
            targetPosition - spotlight.position;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );

        float smoothing =
            1f - Mathf.Exp(
                -rotationSmoothness *
                Time.unscaledDeltaTime
            );

        spotlight.rotation =
            Quaternion.Slerp(
                spotlight.rotation,
                targetRotation,
                smoothing
            );
    }

    private void OnDrawGizmosSelected()
    {
        if (curtainAimCenter == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            leftAimPosition,
            0.15f
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            rightAimPosition,
            0.15f
        );

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(
            curtainAimCenter.position,
            new Vector3(
                horizontalRange * 2f,
                verticalRange * 2f,
                0.05f
            )
        );
    }
}
