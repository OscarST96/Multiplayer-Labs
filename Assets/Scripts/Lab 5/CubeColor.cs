using UnityEngine;
using Unity.Netcode;

public class CubeColor : NetworkBehaviour
{
    private Color[] COLORS = { Color.red, Color.green, Color.blue, Color.yellow };

    private Vector3 spawnPoint;

    public override void OnNetworkSpawn()
    {
        print(NetworkObjectId);

        GetComponent<Renderer>().material.color = COLORS[NetworkObjectId % 4];
        spawnPoint = transform.position;
    }

}