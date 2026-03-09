using Unity.Netcode;
using UnityEngine;

public class GameUI : NetworkBehaviour
{
    public static bool gameOver = false;
    public static ulong winnerId = 0;

    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        // Connection status
        GUILayout.BeginArea(new Rect(10, 10, 300, 50));
        if (NetworkManager.Singleton.IsHost)
            GUILayout.Label("Mode: HOST");
        else if (NetworkManager.Singleton.IsClient)
            GUILayout.Label("Mode: CLIENT");
        GUILayout.Label("Connected: " + NetworkManager.Singleton.ConnectedClients.Count);
        GUILayout.EndArea();

        // Lives display
        GUILayout.BeginArea(new Rect(10, 70, 300, 300));
        GUILayout.Label("=== LIVES ===");

        // Find all players every frame
        PlayerHealth[] players = FindObjectsOfType<PlayerHealth>();

        if (players.Length == 0)
        {
            GUILayout.Label("Waiting for players...");
        }
        else
        {
            foreach (var player in players)
            {
                if (player == null) continue;
                string you = player.IsOwner ? " (You)" : "";
                int hearts = Mathf.Clamp(player.lives.Value, 0, 3);
                GUILayout.Label($"Player {player.OwnerClientId}{you}: " +
                    new string('♥', hearts) +
                    new string('♡', 3 - hearts));
            }
        }

        GUILayout.EndArea();

        // Game over overlay
        if (gameOver)
        {
            GUILayout.BeginArea(new Rect(Screen.width / 2 - 100,
                Screen.height / 2 - 25, 200, 50));
            bool isWinner = NetworkManager.Singleton.LocalClientId == winnerId;
            GUILayout.Label(isWinner ? "YOU WIN!" : "YOU LOSE!");
            GUILayout.EndArea();
        }
    }
}