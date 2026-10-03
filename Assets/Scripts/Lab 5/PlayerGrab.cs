using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGrab : NetworkBehaviour
{
    [Header("Grab Settings")]
    [SerializeField] private float grabDistance = 3f;
    [SerializeField] private LayerMask grabbableLayer;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float throwForce = 10f;

    private GrabbableCube currentlyHeldCube;
    private Camera mainCamera;

    public override void OnNetworkSpawn()
    {
        mainCamera = Camera.main;
        if (holdPoint == null)
        {
            holdPoint = transform.Find("HoldPoint");
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current.eKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentlyHeldCube == null)
            {
                TryGrab();
            }
            else
            {
                TryRelease();
            }
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && currentlyHeldCube != null)
        {
            TryThrow();
        }
    }

    private void TryGrab()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        if (Physics.Raycast(ray, out RaycastHit hit, grabDistance, grabbableLayer))
        {
            if (hit.collider.TryGetComponent(out GrabbableCube cube))
            {
                cube.TryGrabServerRpc(OwnerClientId, holdPoint.position, holdPoint.rotation);
                currentlyHeldCube = cube;
            }
        }
    }

    private void TryRelease()
    {
        if (currentlyHeldCube != null)
        {
            currentlyHeldCube.TryReleaseServerRpc(OwnerClientId);
            currentlyHeldCube = null;
        }
    }

    private void TryThrow()
    {
        if (currentlyHeldCube != null && currentlyHeldCube.TryGetComponent(out Rigidbody rb))
        {
            currentlyHeldCube.TryReleaseServerRpc(OwnerClientId);
            
            Vector3 throwDirection = mainCamera.transform.forward;
            ThrowCubeServerRpc(currentlyHeldCube.GetComponent<NetworkObject>(), throwDirection * throwForce);
            
            currentlyHeldCube = null;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ThrowCubeServerRpc(NetworkObjectReference cubeRef, Vector3 force)
    {
        if (cubeRef.TryGet(out NetworkObject cubeObj) && cubeObj.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = false;
            rb.AddForce(force, ForceMode.Impulse);
            
            if (cubeObj.TryGetComponent(out NetworkRigidbody nr))
            {
                nr.SetIsKinematic(false);
            }
        }
    }

    public void ForceReleaseHeldCube()
    {
        if (currentlyHeldCube != null)
        {
            currentlyHeldCube.ForceRelease();
            currentlyHeldCube = null;
        }
    }
}