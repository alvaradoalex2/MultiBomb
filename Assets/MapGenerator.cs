using Unity.Netcode;
using UnityEngine;

public class MapGenerator : NetworkBehaviour
{
    [Header("Map Settings")]
    public int width = 13;
    public int height = 11;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject solidWallPrefab;
    public GameObject breakableWallPrefab;

    private int seed = 42;

    public override void OnNetworkSpawn()
    {
        // Every instance generates the same map using the fixed seed
        Random.InitState(seed);
        GenerateMap();
    }

    void GenerateMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x, 0, z);

                Instantiate(floorPrefab, pos, Quaternion.identity);

                if (x == 0 || x == width - 1 || z == 0 || z == height - 1)
                {
                    Instantiate(solidWallPrefab, pos + Vector3.up, Quaternion.identity);
                }
                else if (x % 2 == 0 && z % 2 == 0)
                {
                    Instantiate(solidWallPrefab, pos + Vector3.up, Quaternion.identity);
                }
                else if (!IsPlayerSpawnZone(x, z) && Random.value < 0.4f)
                {
                    Instantiate(breakableWallPrefab, pos + Vector3.up, Quaternion.identity);
                }
            }
        }
    }

    bool IsPlayerSpawnZone(int x, int z)
    {
        bool topLeft = x <= 2 && z <= 2;
        bool topRight = x >= width - 3 && z <= 2;
        bool bottomLeft = x <= 2 && z >= height - 3;
        bool bottomRight = x >= width - 3 && z >= height - 3;

        return topLeft || topRight || bottomLeft || bottomRight;
    }
}