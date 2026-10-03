using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class NetworkTransformSetup : MonoBehaviour
{
    [Header("Avatar Settings")]
    public bool configureAvatar = true;
    public GameObject avatarPrefab;

    [Header("Chest Settings")]
    public bool configureChest = true;
    public GameObject chestPrefab;

    [Header("Grabbable Cube Settings")]
    public bool configureGrabbableCube = true;
    public GameObject grabbableCubePrefab;

    [Header("Throwable Cube Settings")]
    public bool configureThrowableCube = true;
    public GameObject throwableCubePrefab;

    private void Awake()
    {
        if (configureAvatar && avatarPrefab != null)
            ConfigureAvatar(avatarPrefab);
        
        if (configureChest && chestPrefab != null)
            ConfigureChest(chestPrefab);
        
        if (configureGrabbableCube && grabbableCubePrefab != null)
            ConfigureGrabbableCube(grabbableCubePrefab);
        
        if (configureThrowableCube && throwableCubePrefab != null)
            ConfigureThrowableCube(throwableCubePrefab);
    }

    private void ConfigureAvatar(GameObject prefab)
    {
        var nt = prefab.GetComponent<NetworkTransform>();
        if (nt == null) return;

        nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        
        nt.SyncPositionX = true;
        nt.SyncPositionY = false;
        nt.SyncPositionZ = true;
        
        nt.SyncRotAngleX = false;
        nt.SyncRotAngleY = true;
        nt.SyncRotAngleZ = false;
        
        nt.SyncScaleX = false;
        nt.SyncScaleY = false;
        nt.SyncScaleZ = false;

        nt.UseUnreliableDeltas = true;
        nt.UseHalfFloatPrecision = true;
        
        nt.Interpolate = true;
        nt.SlerpPosition = false;
        
        nt.SwitchTransformSpaceWhenParented = true;

        Debug.Log("[NetworkTransformSetup] Avatar configured");
    }

    private void ConfigureChest(GameObject prefab)
    {
        var nt = prefab.GetComponent<NetworkTransform>();
        if (nt == null) return;

        nt.AuthorityMode = NetworkTransform.AuthorityModes.Server;
        nt.UseUnreliableDeltas = true;
        nt.UseHalfFloatPrecision = true;
        nt.Interpolate = true;
        
        Debug.Log("[NetworkTransformSetup] Chest configured");
    }

    private void ConfigureGrabbableCube(GameObject prefab)
    {
        var nt = prefab.GetComponent<NetworkTransform>();
        if (nt == null) return;

        nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        nt.UseUnreliableDeltas = true;
        nt.UseHalfFloatPrecision = true;
        nt.Interpolate = true;
        nt.SlerpPosition = false;
        nt.SwitchTransformSpaceWhenParented = true;

        var nr = prefab.GetComponent<NetworkRigidbody>();
        if (nr != null)
        {
            nr.UseRigidBodyForMotion = false;
            nr.AutoUpdateKinematicState = true;
            nr.AutoSetKinematicOnDespawn = true;
        }

        Debug.Log("[NetworkTransformSetup] GrabbableCube configured");
    }

    private void ConfigureThrowableCube(GameObject prefab)
    {
        var nt = prefab.GetComponent<NetworkTransform>();
        if (nt == null) return;

        nt.AuthorityMode = NetworkTransform.AuthorityModes.Server;
        nt.UseUnreliableDeltas = true;
        nt.UseHalfFloatPrecision = true;
        nt.Interpolate = true;
        nt.SlerpPosition = false;
        nt.SwitchTransformSpaceWhenParented = true;

        var nr = prefab.GetComponent<NetworkRigidbody>();
        if (nr != null)
        {
            nr.UseRigidBodyForMotion = true;
            nr.AutoUpdateKinematicState = true;
            nr.AutoSetKinematicOnDespawn = true;
        }

        Debug.Log("[NetworkTransformSetup] ThrowableCube configured");
    }
}