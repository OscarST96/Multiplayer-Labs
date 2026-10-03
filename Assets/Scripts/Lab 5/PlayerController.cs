using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float speed = 5.0f;

    private float x;
    private float y;

    void Update()
    {
        if (!IsOwner) return;
        PlayerMov();
    }
    private void PlayerMov()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");

        if(x == 0 && y == 0) return;

        Vector3 dir = new Vector3 (x, 0, y);
        transform.position += dir * speed * Time.deltaTime;

        PlayerMovServerRPC(transform.position);
    }
    [ServerRpc]
    public void PlayerMovServerRPC(Vector3 position)
    {
        PlayerMovrClientRPC(position);
    }
    [ClientRpc]
    public void PlayerMovrClientRPC(Vector3 position)
    {
        if (IsOwner) return;

        transform.position = position;
    }
}
