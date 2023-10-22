using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NetworkSpawnObject
{
	ROAD = 0,
	T_SPLIT_ROAD = 1,
	CROSS_ROAD = 2,
	TURN_ROAD = 3,
	BUILDING = 4,
	SHOP = 5
}

[CreateAssetMenu(menuName = "My Assets/NetworkSpawnInfo")]
public class NetworkSpawnInfo : ScriptableObject
{
	// TODO: A serializableDictionary would help here...
	public List<GameObject> prefabList = new List<GameObject>();
}
