using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class BombController : NetworkBehaviour
{
    // How long before the bomb explodes
    public float explosionDelay = 3f;
    // How far the explosion reaches in each direction
    public int explosionRange = 2;
    // The explosion visual prefab
    public GameObject explosionPrefab;

    // NetworkVariable tracks explosion state - synced to all clients
    private NetworkVariable<bool> hasExploded = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // Only the server controls explosion timing
        if (IsServer)
            StartCoroutine(ExplodeAfterDelay());
    }

    IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);
        Explode();
    }

    void Explode()
    {
        if (hasExploded.Value) return;
        hasExploded.Value = true;

        Vector3[] directions = {
            Vector3.forward, Vector3.back,
            Vector3.left, Vector3.right
        };

        // Spawn explosion at bomb's own position
        SpawnExplosionClientRpc(transform.position);

        foreach (var dir in directions)
        {
            for (int i = 1; i <= explosionRange; i++)
            {
                Vector3 pos = transform.position + dir * i;
                Collider[] hits = Physics.OverlapSphere(pos, 0.4f);
                bool blocked = false;

                foreach (var hit in hits)
                {
                    // Breakable wall - destroy on all clients and stop explosion
                    if (hit.CompareTag("Breakable"))
                    {
                        DestroyBreakableClientRpc(pos);
                        blocked = true;
                        break;
                    }

                    // Solid wall - block explosion but don't destroy
                    if (hit.CompareTag("Wall"))
                    {
                        blocked = true;
                        break;
                    }

                    // Player hit - deal damage via ServerRpc
                    PlayerHealth health = hit.GetComponent<PlayerHealth>();
                    if (health != null)
                        health.TakeDamageServerRpc();
                }

                // Spawn explosion visual at this position on all clients
                SpawnExplosionClientRpc(pos);
                if (blocked) break;
            }
        }

        // Despawn the bomb on all clients
        DespawnBombClientRpc();
    }

    // Tells all clients to show explosion visual at position
    [ClientRpc]
    void SpawnExplosionClientRpc(Vector3 position)
    {
        if (explosionPrefab != null)
        {
            GameObject exp = Instantiate(explosionPrefab, position, Quaternion.identity);
            // Auto destroy explosion visual after 1 second
            Destroy(exp, 1f);
        }
    }

    // Tells all clients to find and destroy breakable wall at position
    [ClientRpc]
    void DestroyBreakableClientRpc(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, 0.4f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Breakable"))
            {
                Destroy(hit.gameObject);
                break;
            }
        }
    }

    // Tells all clients to despawn the bomb
    [ClientRpc]
    void DespawnBombClientRpc()
    {
        if (IsServer)
            GetComponent<NetworkObject>().Despawn(true);
    }
}