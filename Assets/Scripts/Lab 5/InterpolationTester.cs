using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;

public class InterpolationTester : NetworkBehaviour
{
    [Header("Interpolation Settings")]
    [SerializeField] private NetworkTransform networkTransform;
    [SerializeField] private float simulatedLatencyMs = 200f;
    [SerializeField] private bool enableLatencySimulation = true;

    [Header("UI References (Optional)")]
    [SerializeField] private Text statusText;
    [SerializeField] private Dropdown interpolationDropdown;

    private bool currentSlerpPosition;
    private float originalPositionThreshold;
    private float originalRotationThreshold;

    private enum InterpolationMode
    {
        Lerp = 0,
        Slerp = 1
    }

    public override void OnNetworkSpawn()
    {
        if (networkTransform == null)
            networkTransform = GetComponent<NetworkTransform>();

        if (networkTransform != null)
        {
            originalPositionThreshold = networkTransform.PositionThreshold;
            originalRotationThreshold = networkTransform.RotAngleThreshold;
            currentSlerpPosition = networkTransform.SlerpPosition;

            if (enableLatencySimulation && IsServer)
            {
                StartCoroutine(SimulateLatency());
            }
        }

        SetupUI();
    }

    private void SetupUI()
    {
        if (interpolationDropdown != null)
        {
            interpolationDropdown.ClearOptions();
            interpolationDropdown.AddOptions(new System.Collections.Generic.List<string> 
            { 
                "Lerp (Linear)", 
                "Slerp (Spherical)" 
            });
            interpolationDropdown.value = currentSlerpPosition ? 1 : 0;
            interpolationDropdown.onValueChanged.AddListener(OnInterpolationChanged);
        }
        UpdateStatusText();
    }

    private void OnInterpolationChanged(int index)
    {
        if (networkTransform == null) return;

        bool useSlerp = index == 1;
        networkTransform.SlerpPosition = useSlerp;
        currentSlerpPosition = useSlerp;
        
        UpdateStatusText();
        
        if (IsServer)
        {
            SyncInterpolationTypeClientRpc(useSlerp);
        }
    }

    [ClientRpc]
    private void SyncInterpolationTypeClientRpc(bool useSlerp)
    {
        if (networkTransform == null) return;
        
        networkTransform.SlerpPosition = useSlerp;
        currentSlerpPosition = useSlerp;
        UpdateStatusText();
    }

    private System.Collections.IEnumerator SimulateLatency()
    {
        while (enableLatencySimulation)
        {
            yield return new WaitForSeconds(simulatedLatencyMs / 1000f);
            
            if (networkTransform != null && IsServer)
            {
                Teleport(
                    networkTransform.transform.position,
                    networkTransform.transform.rotation,
                    networkTransform.transform.localScale
                );
            }
        }
    }

    public void SetInterpolationType(bool useSlerp)
    {
        if (networkTransform == null) return;
        
        networkTransform.SlerpPosition = useSlerp;
        currentSlerpPosition = useSlerp;
        UpdateStatusText();
    }

    public void SetLatency(float latencyMs)
    {
        simulatedLatencyMs = Mathf.Max(0f, latencyMs);
        UpdateStatusText();
    }

    public void ToggleLatencySimulation(bool enabled)
    {
        enableLatencySimulation = enabled;
        if (enabled && IsServer)
        {
            StartCoroutine(SimulateLatency());
        }
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            string interpName = currentSlerpPosition ? "Slerp" : "Lerp";
            statusText.text = $"Interpolation: {interpName}\nSimulated Latency: {simulatedLatencyMs}ms\nEnabled: {enableLatencySimulation}";
        }
    }

    private void OnValidate()
    {
        if (networkTransform == null)
            networkTransform = GetComponent<NetworkTransform>();
    }

    private void Teleport(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (networkTransform == null) return;
        
        networkTransform.Teleport(position, rotation, scale);
    }
}