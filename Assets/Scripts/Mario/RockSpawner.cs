using System.Collections;
using UnityEngine;

public class RockSpawner : MonoBehaviour
{
    [SerializeField] private GameObject rockPrefab; // Prefab de la roca que cae
    [SerializeField] private GameObject warningPrefab; // Prefab con RockWarningIndicator que avisa dónde caerá la roca
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask groundLayer; // Layer "Ground", usada para ubicar el aviso y el impacto justo sobre el suelo
    [SerializeField] private Camera spawnCamera; // Cámara de referencia; si se deja vacío usa Camera.main
    [SerializeField, Min(0.1f)] private float spawnInterval = 3f; // Tiempo entre caídas de roca
    [SerializeField, Min(0.1f)] private float warningDuration = 2f; // Tiempo que el aviso sigue al jugador antes de que caiga la roca
    [SerializeField] private float heightAboveCamera = 15f; // Cuánto más arriba de la cámara aparece la roca, para que se vea caer

    private Transform player;
    private float spawnTimer;

    private void Awake()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            player = playerObj.transform;

        if (spawnCamera == null)
            spawnCamera = Camera.main;
    }

    private void Update()
    {
        if (player == null || rockPrefab == null) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            StartCoroutine(SpawnRockWithWarning());
        }
    }

    // Busca el punto de suelo justo debajo de una posición, para que el aviso
    // y la roca queden alineados con el suelo real, aunque el jugador esté
    // sobre una plataforma o escalón.
    private Vector3 GetGroundPosition(Vector3 fromPosition)
    {
        if (Physics.Raycast(fromPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
            return hit.point;

        return fromPosition;
    }

    private IEnumerator SpawnRockWithWarning()
    {
        // 1) El aviso aparece sobre el jugador y lo sigue mientras se mueve.
        GameObject warningObj = null;
        if (warningPrefab != null)
        {
            warningObj = Instantiate(warningPrefab, GetGroundPosition(player.position), Quaternion.identity);
            RockWarningIndicator warning = warningObj.GetComponent<RockWarningIndicator>();
            if (warning != null)
                warning.Follow(player, groundLayer);
        }

        yield return new WaitForSeconds(warningDuration);

        // 2) La roca cae sobre la posición actual del jugador (en vivo, no la de hace 2 segundos),
        //    apareciendo arriba de la cámara para que se vea venir antes de llegar.
        Vector3 targetPosition = GetGroundPosition(player.position);
        Vector3 spawnPosition = targetPosition;
        spawnPosition.y = (spawnCamera != null ? spawnCamera.transform.position.y : targetPosition.y) + heightAboveCamera;

        Instantiate(rockPrefab, spawnPosition, Quaternion.identity);

        if (warningObj != null)
            Destroy(warningObj);
    }
}
