using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public class PlayerAvatar : NetworkBehaviour
{ 
    [SerializeField] private float moveSpeed = 5f;
    [Header("NetworkTransform Config")]
    [SerializeField] private bool configureOnSpawn = true;

    private Rigidbody rb;
    private Vector2 moveInput;
    private NetworkTransform networkTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkTransform = GetComponent<NetworkTransform>();
        
        if (configureOnSpawn)
        {
            ConfigureNetworkTransform();
        }
    }

    private void ConfigureNetworkTransform()
    {
        if (networkTransform == null) return;

        networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        
        networkTransform.SyncPositionX = true;
        networkTransform.SyncPositionY = false;
        networkTransform.SyncPositionZ = true;
        
        networkTransform.SyncRotAngleX = false;
        networkTransform.SyncRotAngleY = true;
        networkTransform.SyncRotAngleZ = false;
        
        networkTransform.SyncScaleX = false;
        networkTransform.SyncScaleY = false;
        networkTransform.SyncScaleZ = false;

        networkTransform.UseUnreliableDeltas = true;
        networkTransform.UseHalfFloatPrecision = true;
        
        networkTransform.Interpolate = true;
        networkTransform.SlerpPosition = false;
        
        networkTransform.SwitchTransformSpaceWhenParented = true;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Keyboard.current == null)
            return;

        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed)
            x = -1f;

        if (Keyboard.current.dKey.isPressed)
            x = 1f;

        if (Keyboard.current.wKey.isPressed)
            z = 1f;

        if (Keyboard.current.sKey.isPressed)
            z = -1f;

        moveInput = new Vector2(x, z).normalized;

        SendMovementServerRpc(moveInput);
    }

    [ServerRpc]
    private void SendMovementServerRpc(Vector2 input)
    {
        Vector3 movement = new Vector3(
            input.x,
            0f,
            input.y
        );

        rb.linearVelocity = new Vector3(
            movement.x * moveSpeed,
            rb.linearVelocity.y,
            movement.z * moveSpeed
        );
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log("Soy el jugador local: " + OwnerClientId);
        }
    }
}