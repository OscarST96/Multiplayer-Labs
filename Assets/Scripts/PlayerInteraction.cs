using Unity.Netcode;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Interacción")]
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private LayerMask resourceLayer;

    // Paleta de colores asignada por ID de Cliente
    private readonly Color[] playerColors = new Color[]
    {
        Color.green,   // Cliente 0
        Color.red,     // Cliente 1
        Color.blue,    // Cliente 2
        Color.yellow,  // Cliente 3
        Color.magenta  // Cliente 4
    };

    public Color PlayerColor { get; private set; }

    public override void OnNetworkSpawn()
    {
        // El color del jugador se asigna según su ID en la red
        int colorIndex = (int)OwnerClientId % playerColors.Length;
        PlayerColor = playerColors[colorIndex];
    }

    private void Update()
    {
        // Solo el jugador local procesa entradas del teclado/mouse
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, resourceLayer))
        {
            if (hit.collider.TryGetComponent(out TreeNode tree))
            {
                // 3️⃣ Enviar la solicitud de reclamo al servidor
                tree.RequestClaimResourceServerRpc();
            }
        }
    }
}