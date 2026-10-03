using Unity.Netcode;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Interacción")]
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private LayerMask resourceLayer;
    [SerializeField] private LayerMask chestLayer;

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
        int colorIndex = (int)OwnerClientId % playerColors.Length;
        PlayerColor = playerColors[colorIndex];
    }

    private void Update()
    {
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
                tree.RequestClaimResourceServerRpc();
            }
        }
        else if (Physics.Raycast(ray, out RaycastHit chestHit, interactDistance, chestLayer))
        {
            if (chestHit.collider.TryGetComponent(out Chest chest))
            {
                if (chest.CanOpen(OwnerClientId))
                {
                    chest.TryOpenChestServerRpc(OwnerClientId);
                }
                else
                {
                    Debug.Log($"[PlayerInteraction] Chest already opened by client {chest.OpenerClientId}");
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Camera.main == null) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Vector3 origin = Camera.main.transform.position;
        Vector3 direction = Camera.main.transform.forward;
        Gizmos.DrawRay(origin, direction * interactDistance);
        Gizmos.DrawWireSphere(origin + direction * interactDistance, 0.1f);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawFrustum(origin, Camera.main.fieldOfView, interactDistance, 0.01f, Camera.main.aspect);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(ray.origin, ray.direction * interactDistance);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, resourceLayer | chestLayer))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hit.point, 0.15f);
            Gizmos.DrawLine(ray.origin, hit.point);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(ray.origin + ray.direction * interactDistance, 0.1f);
        }
    }
}