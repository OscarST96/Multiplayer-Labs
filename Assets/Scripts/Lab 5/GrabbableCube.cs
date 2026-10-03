using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class GrabbableCube : NetworkBehaviour
{
    [Header("Grabbable Settings")]
    [SerializeField] private float grabDistance = 2f;
    [SerializeField] private bool usePhysicsWhenHeld = false;
    
    [Header("NetworkTransform Config")]
    [SerializeField] private bool configureOnSpawn = true;

    private NetworkTransform networkTransform;
    private NetworkRigidbody networkRigidbody;
    private Rigidbody rb;
    private ulong currentHolderId = 0;
    private bool isHeld = false;
    private Vector3 localHoldOffset;
    private Quaternion localHoldRotation;

    public bool IsHeld => isHeld;
    public ulong HolderId => currentHolderId;

    public override void OnNetworkSpawn()
    {
        networkTransform = GetComponent<NetworkTransform>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
        rb = GetComponent<Rigidbody>();

        if (configureOnSpawn && IsServer)
        {
            ConfigureNetworkTransform();
        }

        if (IsServer && networkTransform != null)
        {
            networkTransform.SwitchTransformSpaceWhenParented = true;
        }
    }

    private void ConfigureNetworkTransform()
    {
        if (networkTransform == null) return;

        networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        networkTransform.UseUnreliableDeltas = true;
        networkTransform.UseHalfFloatPrecision = true;
        networkTransform.Interpolate = true;
        networkTransform.SlerpPosition = false;
        networkTransform.SwitchTransformSpaceWhenParented = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TryGrabServerRpc(ulong clientId, Vector3 grabberPosition, Quaternion grabberRotation)
    {
        if (isHeld)
        {
            TryGrabFailedClientRpc(clientId, "Already held by another player");
            return;
        }

        float distance = Vector3.Distance(transform.position, grabberPosition);
        if (distance > grabDistance)
        {
            TryGrabFailedClientRpc(clientId, "Too far to grab");
            return;
        }

        isHeld = true;
        currentHolderId = clientId;
        
        NetworkObject holderObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        if (holderObj != null)
        {
            Transform holderTransform = holderObj.transform.Find("HoldPoint");
            if (holderTransform != null)
            {
                // Snap to HoldPoint: zero local offset
                localHoldOffset = Vector3.zero;
                localHoldRotation = Quaternion.identity;
                
                TrySetParentClientRpc(clientId, true, localHoldOffset, localHoldRotation);
                
                if (networkRigidbody != null)
                {
                    networkRigidbody.SetIsKinematic(true);
                }
            }
            else
            {
                Debug.LogWarning($"[GrabbableCube] No HoldPoint found on player {clientId}");
                TryGrabFailedClientRpc(clientId, "Player missing HoldPoint");
                isHeld = false;
                currentHolderId = 0;
            }
        }
        
        Debug.Log($"[GrabbableCube] Grabbed by client {clientId}");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TryReleaseServerRpc(ulong clientId)
    {
        if (!isHeld || currentHolderId != clientId)
        {
            return;
        }

        ReleaseCube(clientId);
    }

    private void ReleaseCube(ulong clientId)
    {
        isHeld = false;
        currentHolderId = 0;
        
        TrySetParentClientRpc(clientId, false, Vector3.zero, Quaternion.identity);
        
        if (networkRigidbody != null)
        {
            networkRigidbody.SetIsKinematic(false);
        }
        
        Debug.Log($"[GrabbableCube] Released by client {clientId}");
    }

    [ClientRpc]
    private void TrySetParentClientRpc(ulong clientId, bool parent, Vector3 localOffset, Quaternion localRotation)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId)
            return;

        NetworkObject holderObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        if (holderObj == null)
            return;

        Transform holderRoot = holderObj.transform;
        Transform holdPoint = holderRoot.Find("HoldPoint");
        if (holdPoint == null)
        {
            Debug.LogWarning($"[GrabbableCube] No HoldPoint found on player {clientId}");
            return;
        }

        if (parent)
        {
            // Parent to the NetworkObject root (required by Netcode)
            // NetworkTransform with SwitchTransformSpaceWhenParented handles local space conversion
            transform.SetParent(holderRoot, true);
            
            // Position relative to HoldPoint in world space, then NetworkTransform converts to local
            transform.position = holdPoint.TransformPoint(localOffset);
            transform.rotation = holdPoint.rotation * localRotation;
            
            if (rb != null && usePhysicsWhenHeld)
            {
                rb.isKinematic = false;
            }
            else if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
        else
        {
            transform.SetParent(null, true);
            
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
    }

    [ClientRpc]
    private void TryGrabFailedClientRpc(ulong clientId, string reason)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Debug.LogWarning($"[GrabbableCube] Grab failed: {reason}");
        }
    }

    public void ForceRelease()
    {
        if (IsServer && isHeld)
        {
            ReleaseCube(currentHolderId);
        }
    }
}
