using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public GameObject playerPrefab;
    private HashSet<ulong> spawnedClients = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        // Absolutely prevent double spawning
        if (spawnedClients.Contains(clientId))
        {
           // Debug.Log($"Already spawned client {clientId}, skipping");
            return;
        }

        spawnedClients.Add(clientId);
        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
       // Debug.Log($"Spawned player for client {clientId}");
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }
}