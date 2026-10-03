using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThrow : NetworkBehaviour
{
    [Header("Throw Settings")]
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float pickupDistance = 3f;
    [SerializeField] private LayerMask throwableLayer;
    [SerializeField] private Transform throwPoint;

    private Camera mainCamera;
    private ThrowableCube heldThrowable;

    public override void OnNetworkSpawn()
    {
        mainCamera = Camera.main;
        if (throwPoint == null)
        {
            throwPoint = transform.Find("ThrowPoint");
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current.eKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (heldThrowable == null)
            {
                TryPickup();
            }
            else
            {
                TryThrow();
            }
        }

        if (Keyboard.current.rKey.wasPressedThisFrame && heldThrowable != null)
        {
            RequestRespawn();
        }
    }

    private void TryPickup()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, throwableLayer))
        {
            if (hit.collider.TryGetComponent(out ThrowableCube throwable))
            {
                heldThrowable = throwable;
                heldThrowable.ThrowServerRpc(mainCamera.transform.forward, 1f);
                heldThrowable = null;
            }
        }
    }

    private void TryThrow()
    {
        if (heldThrowable != null)
        {
            heldThrowable.ThrowServerRpc(mainCamera.transform.forward, 1f);
            heldThrowable = null;
        }
    }

    private void RequestRespawn()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, throwableLayer))
        {
            if (hit.collider.TryGetComponent(out ThrowableCube throwable))
            {
                throwable.RequestRespawnServerRpc();
            }
        }
    }
}