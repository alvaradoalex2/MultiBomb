using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkManagerUI : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Singleton == null) return;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

#if UNITY_EDITOR
        transport.SetConnectionData("127.0.0.1", 7777);
        NetworkManager.Singleton.StartHost();
#else
            transport.SetConnectionData("127.0.0.1", 7777);
            StartCoroutine(DelayedClientStart());
#endif
    }

    System.Collections.IEnumerator DelayedClientStart()
    {
        yield return new WaitForSeconds(2f);
        NetworkManager.Singleton.StartClient();
    }
}