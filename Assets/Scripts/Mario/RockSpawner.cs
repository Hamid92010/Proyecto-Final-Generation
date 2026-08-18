using System.Collections;
using UnityEngine;

public class RockSpawner : MonoBehaviour
{
    [SerializeField] private GameObject rockPrefab; // Prefab de la roca que cae
    [SerializeField] private GameObject warningPrefab; // Marca visual que avisa dónde caerá la roca
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask groundLayer; // Layer "Ground", usada para ubicar el aviso justo sobre el suelo
    [SerializeField] private Camera spawnCamera; // Cámara de referencia; si se deja vacío usa Camera.main
    [SerializeField, Min(0.1f)] private float spawnInterval = 3f; // Tiempo entre caídas de roca
    [SerializeField, Min(0.1f)] private float warningDuration = 1.5f; // Tiempo que se ve la marca en el suelo antes de que aparezca la roca
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
            StartCoroutine(SpawnRockWithWarning(GetGroundPosition(player.position)));
        }
    }

    // Busca el punto de suelo justo debajo del jugador para que el aviso
    // y la caída de la roca queden alineados con la posición real, aunque
    // el jugador esté sobre una plataforma o escalón.
    private Vector3 GetGroundPosition(Vector3 fromPosition)
    {
        if (Physics.Raycast(fromPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
            return hit.point;

        return fromPosition;
    }

    private IEnumerator SpawnRockWithWarning(Vector3 targetPosition)
    {
        // 1) Se marca primero el lugar donde va a caer.
        GameObject warning = warningPrefab != null ? Instantiate(warningPrefab, targetPosition, Quaternion.identity) : null;

        yield return new WaitForSeconds(warningDuration);

        // 2) La roca aparece arriba de la cámara, para que el jugador la vea caer
        //    antes de que llegue a su posición, y no justo encima suyo.
        Vector3 spawnPosition = targetPosition;
        spawnPosition.y = (spawnCamera != null ? spawnCamera.transform.position.y : targetPosition.y) + heightAboveCamera;

        Instantiate(rockPrefab, spawnPosition, Quaternion.identity);

        // El aviso se queda marcando el punto de impacto hasta que la roca desaparece
        // (se destruye en FallingRock al chocar con el jugador o con el suelo).
        Destroy(warning, 10f);
    }
}
