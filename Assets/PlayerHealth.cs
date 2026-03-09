using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    // NetworkVariable syncs lives to all clients automatically
    public NetworkVariable<int> lives = new NetworkVariable<int>(
        3,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private static readonly Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(1, 1, 1),
        new Vector3(11, 1, 9),
        new Vector3(11, 1, 1),
        new Vector3(1, 1, 9),
    };

    public override void OnNetworkSpawn()
    {
        lives.OnValueChanged += OnLivesChanged;
    }

    void OnLivesChanged(int oldVal, int newVal)
    {
      //  Debug.Log($"Player {OwnerClientId} lives changed: {oldVal} -> {newVal}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc()
    {
        if (!IsServer) return;
        if (lives.Value <= 0) return;

        lives.Value--;
       // Debug.Log($"Player {OwnerClientId} took damage, lives remaining: {lives.Value}");

        if (lives.Value <= 0)
        {
           // Debug.Log($"Player {OwnerClientId} eliminated, checking win condition");
            CheckWinCondition();
        }
        else
        {
            RespawnClientRpc();
        }
    }

    void CheckWinCondition()
    {
        PlayerHealth[] allPlayers = FindObjectsOfType<PlayerHealth>();

        int alivePlayers = 0;
        ulong winnerId = 0;

        foreach (var p in allPlayers)
        {
            // Count players still alive excluding this one
            if (p != this && p.lives.Value > 0)
            {
                alivePlayers++;
                winnerId = p.OwnerClientId;
               // Debug.Log($"Player {p.OwnerClientId} is still alive");
            }
        }

       // Debug.Log($"Alive players remaining: {alivePlayers}");

        if (alivePlayers <= 1)
        {
           // Debug.Log($"Game over! Winner: {winnerId}");
            AnnounceWinnerClientRpc(winnerId);
        }

        // Despawn eliminated player
        GetComponent<NetworkObject>().Despawn(true);
    }

    [ClientRpc]
    void RespawnClientRpc()
    {
        if (!IsOwner) return;
        int index = (int)OwnerClientId % spawnPositions.Length;
        transform.position = spawnPositions[index];
    }

    [ClientRpc]
    void AnnounceWinnerClientRpc(ulong winnerId)
    {
        //Debug.Log($"AnnounceWinner received, winner: {winnerId}");
        GameUI.winnerId = winnerId;
        GameUI.gameOver = true;
    }

    public override void OnDestroy()
    {
        lives.OnValueChanged -= OnLivesChanged;
    }
}