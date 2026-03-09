using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.Collections;

public class NetworkManagerUI : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Singleton == null) return;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

#if UNITY_EDITOR
        // Editor always starts as Host
        transport.SetConnectionData("0.0.0.0", 7777);
        NetworkManager.Singleton.StartHost();
#else
            // Build always connects as Client to host machine's hotspot IP
            transport.SetConnectionData("192.168.137.1", 7777);
            StartCoroutine(DelayedClientStart());
#endif
    }

    IEnumerator DelayedClientStart()
    {
        // Wait for host to be ready before connecting
        yield return new WaitForSeconds(2f);
        NetworkManager.Singleton.StartClient();
    }

    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 100));

        if (NetworkManager.Singleton.IsHost)
            GUILayout.Label("Mode: HOST");
        else if (NetworkManager.Singleton.IsClient)
            GUILayout.Label("Mode: CLIENT");
        else
            GUILayout.Label("Connecting...");

        GUILayout.Label("Connected clients: " +
            NetworkManager.Singleton.ConnectedClients.Count);

        GUILayout.EndArea();
    }
}