// CameraFollow.cs - poner en MainCamera de la escena
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new(0, 5, -10);
    [SerializeField] private float smooth = 10f;

    private Transform target;

    void LateUpdate()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.transform;
            return;
        }
        transform.position = Vector3.Lerp(transform.position, target.position + offset, smooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}