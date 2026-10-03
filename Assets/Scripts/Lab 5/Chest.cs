using UnityEngine;
using Unity.Netcode;

public class Chest : NetworkBehaviour
{
    [Header("Chest Settings")]
    [SerializeField] private NetworkObject grabbableCubePrefab;
    [SerializeField] private int cubeCount = 5;
    [SerializeField] private float spawnRadius = 2f;
    [SerializeField] private float spawnHeight = 1f;

    [Header("Lock System")]
    private NetworkVariable<ulong> openerClientId = new NetworkVariable<ulong>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool IsOpen => isOpen.Value;
    public ulong OpenerClientId => openerClientId.Value;
    public bool IsLocked => isOpen.Value && openerClientId.Value != 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            openerClientId.OnValueChanged += OnOpenerChanged;
            isOpen.OnValueChanged += OnOpenStateChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            openerClientId.OnValueChanged -= OnOpenerChanged;
            isOpen.OnValueChanged -= OnOpenStateChanged;
        }
    }

    private void OnOpenerChanged(ulong previous, ulong current)
    {
        Debug.Log($"[Chest] Opener changed: {previous} -> {current}");
    }

    private void OnOpenStateChanged(bool previous, bool current)
    {
        Debug.Log($"[Chest] Open state changed: {previous} -> {current}");
        if (current && IsServer)
        {
            SpawnCubes();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TryOpenChestServerRpc(ulong clientId)
    {
        if (isOpen.Value)
        {
            Debug.Log($"[Chest] Already opened by client {openerClientId.Value}");
            return;
        }

        openerClientId.Value = clientId;
        isOpen.Value = true;
        
        Debug.Log($"[Chest] Opened by client {clientId}");
    }

    [ClientRpc]
    private void OpenChestClientRpc()
    {
        if (!IsOwner && !IsServer)
        {
            SpawnCubesLocal();
        }
    }

    private void SpawnCubes()
    {
        for (int i = 0; i < cubeCount; i++)
        {
            float angle = i * (360f / cubeCount);
            Vector3 spawnPoint = transform.position + Quaternion.Euler(0f, angle, 0f) * new Vector3(spawnRadius, spawnHeight, 0f);

            if (grabbableCubePrefab != null)
            {
                NetworkObject cube = Instantiate(grabbableCubePrefab, spawnPoint, Random.rotation);
                cube.Spawn();
            }
        }
        
        GetComponent<NetworkObject>().Despawn(true);
    }

    private void SpawnCubesLocal()
    {
        for (int i = 0; i < cubeCount; i++)
        {
            float angle = i * (360f / cubeCount);
            Vector3 spawnPoint = transform.position + Quaternion.Euler(0f, angle, 0f) * new Vector3(spawnRadius, spawnHeight, 0f);

            if (grabbableCubePrefab != null)
            {
                Instantiate(grabbableCubePrefab, spawnPoint, Random.rotation);
            }
        }
    }

    public bool CanOpen(ulong clientId)
    {
        return !isOpen.Value;
    }
}