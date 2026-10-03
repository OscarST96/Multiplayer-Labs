using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class ThrowableCube : NetworkBehaviour
{
    [Header("Throwable Settings")]
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float respawnHeight = -10f;
    [SerializeField] private Vector3 respawnPosition = new Vector3(0, 5, 0);

    [Header("Network Config")]
    [SerializeField] private bool configureOnSpawn = true;
    [SerializeField] private bool kinematicOnClients = true;

    private NetworkTransform networkTransform;
    private NetworkRigidbody networkRigidbody;
    private Rigidbody rb;
    private bool isThrown = false;

    public override void OnNetworkSpawn()
    {
        networkTransform = GetComponent<NetworkTransform>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
        rb = GetComponent<Rigidbody>();

        if (configureOnSpawn && IsServer)
        {
            ConfigureNetworkComponents();
        }

        if (rb != null && kinematicOnClients && !IsServer)
        {
            rb.isKinematic = true;
        }
    }

    private void ConfigureNetworkComponents()
    {
        if (networkTransform != null)
        {
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            networkTransform.UseUnreliableDeltas = true;
            networkTransform.UseHalfFloatPrecision = true;
            networkTransform.Interpolate = true;
            networkTransform.SlerpPosition = false;
            networkTransform.SwitchTransformSpaceWhenParented = true;
        }

        if (networkRigidbody != null)
        {
            networkRigidbody.UseRigidBodyForMotion = true;
            networkRigidbody.AutoUpdateKinematicState = true;
            networkRigidbody.AutoSetKinematicOnDespawn = true;
        }
    }

    public void Throw(Vector3 direction, float forceMultiplier = 1f)
    {
        if (!IsServer) return;
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(direction.normalized * throwForce * forceMultiplier, ForceMode.Impulse);
            isThrown = true;
            
            if (networkRigidbody != null)
            {
                networkRigidbody.SetIsKinematic(false);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ThrowServerRpc(Vector3 direction, float forceMultiplier = 1f)
    {
        Throw(direction, forceMultiplier);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (isThrown && rb != null)
        {
            if (transform.position.y < respawnHeight)
            {
                TeleportToRespawn();
            }
        }
    }

    private void TeleportToRespawn()
    {
        if (networkTransform != null)
        {
            Teleport(respawnPosition, Quaternion.identity);
        }
        else
        {
            transform.position = respawnPosition;
            transform.rotation = Quaternion.identity;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (networkRigidbody != null)
        {
            networkRigidbody.SetIsKinematic(true);
        }

        isThrown = false;
        
        Debug.Log($"[ThrowableCube] Teleported to respawn position: {respawnPosition}");
    }

    private void Teleport(Vector3 position, Quaternion rotation)
    {
        if (networkTransform != null)
        {
            networkTransform.Teleport(position, rotation, transform.localScale);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestRespawnServerRpc()
    {
        TeleportToRespawn();
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, Quaternion rotation)
    {
        Teleport(position, rotation);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetRespawnPosition(Vector3 position)
    {
        if (IsServer)
        {
            respawnPosition = position;
        }
    }

    public void SetRespawnHeight(float height)
    {
        if (IsServer)
        {
            respawnHeight = height;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Track collision if needed
    }

    private void OnValidate()
    {
        if (networkTransform == null)
            networkTransform = GetComponent<NetworkTransform>();
        if (networkRigidbody == null)
            networkRigidbody = GetComponent<NetworkRigidbody>();
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }
}
