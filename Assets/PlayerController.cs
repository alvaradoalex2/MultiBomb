using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gridSize = 1f;

    [Header("Bombs")]
    public GameObject bombPrefab;
    public float bombCooldown = 1f;
    public int maxBombs = 3;

    private bool isMoving = false;
    private Vector3 targetPosition;
    private float lastBombTime = -999f;
    private int activeBombs = 0;

    private static readonly Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(1, 1, 1),   // Host
        new Vector3(11, 1, 9),  // Client 1
        new Vector3(11, 1, 1),  // Client 2
        new Vector3(1, 1, 9),   // Client 3
    };

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        int index = (int)OwnerClientId % spawnPositions.Length;
        Vector3 spawnPos = spawnPositions[index];

        SetSpawnPositionServerRpc(spawnPos);
        targetPosition = spawnPos;
    }

    // Tells server to move player to spawn position
    [ServerRpc]
    private void SetSpawnPositionServerRpc(Vector3 position)
    {
        transform.position = position;
        SetPositionClientRpc(position);
    }

    // Syncs spawn position to all clients
    [ClientRpc]
    private void SetPositionClientRpc(Vector3 position)
    {
        transform.position = position;
        targetPosition = position;
    }

    void Update()
    {
        if (!IsSpawned) return;
        if (!IsOwner) return;
        if (!isMoving) HandleInput();

        // Smooth movement toward grid target
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    void HandleInput()
    {
        Vector3 direction = Vector3.zero;

        // Movement input
        if (Input.GetKey(KeyCode.W)) direction = new Vector3(0, 0, 1);
        else if (Input.GetKey(KeyCode.S)) direction = new Vector3(0, 0, -1);
        else if (Input.GetKey(KeyCode.A)) direction = new Vector3(-1, 0, 0);
        else if (Input.GetKey(KeyCode.D)) direction = new Vector3(1, 0, 0);

        if (direction != Vector3.zero)
        {
            Vector3 newTarget = targetPosition + direction * gridSize;
            if (!IsWallAt(newTarget))
            {
                targetPosition = newTarget;
                isMoving = true;
            }
        }

        // Bomb placement input - Space bar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (activeBombs < maxBombs && Time.time - lastBombTime >= bombCooldown)
            {
                lastBombTime = Time.time;
                activeBombs++;
                PlaceBombServerRpc(transform.position);
            }
        }
    }

    // Checks if a wall exists at given position using physics overlap
    bool IsWallAt(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, 0.4f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Wall") || hit.CompareTag("Breakable"))
                return true;
        }
        return false;
    }

    // Tells server to spawn a bomb at position
    [ServerRpc]
    private void PlaceBombServerRpc(Vector3 position)
    {
        GameObject bombPrefabRef = null;

        // Find bomb prefab from NetworkManager registered prefabs
        foreach (var prefab in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
        {
            if (prefab.Prefab.name == "Bomb")
            {
                bombPrefabRef = prefab.Prefab;
                break;
            }
        }

        if (bombPrefabRef == null)
        {
            Debug.LogError("Bomb prefab not found in NetworkManager prefabs list!");
            return;
        }

        GameObject bomb = Instantiate(bombPrefabRef, position, Quaternion.identity);
        bomb.GetComponent<NetworkObject>().Spawn();

        // Notify owner when bomb expires to free up bomb slot
        StartCoroutine(NotifyBombExpired(bomb.GetComponent<BombController>().explosionDelay));
    }

    // Waits for bomb to explode then notifies owner client
    IEnumerator NotifyBombExpired(float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f);
        BombExpiredClientRpc();
    }

    // Decrements active bomb count on owner client
    [ClientRpc]
    void BombExpiredClientRpc()
    {
        if (IsOwner && activeBombs > 0)
            activeBombs--;
    }
}