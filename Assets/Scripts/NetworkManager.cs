using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    public static uint NextNetworkID => ++nextNetworkId;
    private static uint nextNetworkId = 0;

    [SerializeField]
    private NetworkSpawnInfo spawnInfo;
    private Dictionary<uint, GameObject> networkedReferences = new Dictionary<uint, GameObject>();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Multiple instances of NetworkManager exist! This shouldn't happen!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public bool GetReference(uint id, out GameObject obj)
    {
        obj = null;
        if (networkedReferences.ContainsKey(id))
        {
            obj = networkedReferences[id];
            return true;
        }
        return false;
    }

    public bool SpawnWithId(NetworkSpawnObject type, uint id, out GameObject obj)
    {
        obj = null;
        if (networkedReferences.ContainsKey(id))
        {
            return false;
        }
        else
        {
            // assuming this doesn't crash...
            obj = Instantiate(spawnInfo.prefabList[(int)type]);

            NetworkedBehaviour beh = obj.GetComponent<NetworkedBehaviour>();
            if (beh == null)
            {
                beh = obj.AddComponent<NetworkedBehaviour>();
            }
            beh.NetworkID = id;

            networkedReferences.Add(id, obj);

            return true;
        }
    }

    public bool DestroyWithId(uint id)
    {
        if (networkedReferences.ContainsKey(id))
        {
            Destroy(networkedReferences[id]);
            networkedReferences.Remove(id);
            return true;
        }
        else
        {
            return false;
        }
    }
}