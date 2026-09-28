using Unity.Netcode;
using UnityEngine;

public class TreeNode : NetworkBehaviour
{
    [Header("Componentes Visuales")]
    [SerializeField] private MeshRenderer treeRenderer;

    // 1️⃣ Guardar si el árbol ya fue reclamado (soporta Host y Clientes por igual)
    private readonly NetworkVariable<bool> isClaimed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 2️⃣ Guardar el color actual del árbol
    private readonly NetworkVariable<Color> currentColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (treeRenderer == null)
            treeRenderer = GetComponent<MeshRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        // Sincronizar el color inicial (importante para Late-Join)
        ApplyColor(currentColor.Value);

        // Suscribirse al cambio de color
        currentColor.OnValueChanged += OnColorChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        ApplyColor(newColor);
    }

    private void ApplyColor(Color color)
    {
        if (treeRenderer != null)
        {
            treeRenderer.material.color = color;
        }
    }

    // 3️⃣ Petición del Cliente
    [ServerRpc(RequireOwnership = false)]
    public void RequestClaimResourceServerRpc(ServerRpcParams rpcParams = default)
    {
        // 4️⃣ Validación e Igualdad para Host y Clientes:
        // Si la variable isClaimed ya es TRUE, rechaza a CUALQUIERA (Host o Cliente)
        if (isClaimed.Value)
        {
            Debug.LogWarning($"[Servidor] Petición RECHAZADA: El árbol '{gameObject.name}' ya fue reclamado.");
            return;
        }

        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // Buscar los datos del cliente que hace la petición
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out PlayerInteraction playerSetup))
            {
                // Marcamos que el árbol YA está ocupado
                isClaimed.Value = true;

                // 5️⃣ Cambio de Dueño
                NetworkObject.ChangeOwnership(senderClientId);

                // 5️⃣ Feedback Visual: Sincronizar color a todos los jugadores
                currentColor.Value = playerSetup.PlayerColor;

                Debug.Log($"[Servidor] Árbol reclamado exitosamente por Cliente ID: {senderClientId}");
            }
        }
    }
}
