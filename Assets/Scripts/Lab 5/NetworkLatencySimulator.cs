using Unity.Netcode;
using UnityEngine;

public class NetworkLatencySimulator : NetworkBehaviour
{
    [Header("Latency Simulation")]
    [SerializeField] private float latencyMs = 200f;
    [SerializeField] private bool enabled = true;
    
    private float nextSendTime;
    private System.Collections.Generic.Queue<DelayedMessage> messageQueue = new();
    
    private struct DelayedMessage
    {
        public float sendTime;
        public System.Action action;
    }

    public static NetworkLatencySimulator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && enabled)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"[LatencySim] Simulating {latencyMs}ms latency on server");
        }
    }

    public void QueueDelayedAction(System.Action action)
    {
        if (!enabled || latencyMs <= 0f)
        {
            action?.Invoke();
            return;
        }

        float delay = latencyMs / 1000f;
        messageQueue.Enqueue(new DelayedMessage 
        { 
            sendTime = Time.time + delay, 
            action = action 
        });
    }

    private void Update()
    {
        while (messageQueue.Count > 0 && messageQueue.Peek().sendTime <= Time.time)
        {
            var msg = messageQueue.Dequeue();
            msg.action?.Invoke();
        }
    }

    public void SetLatency(float ms)
    {
        latencyMs = Mathf.Max(0f, ms);
    }

    public void SetEnabled(bool state)
    {
        enabled = state;
    }
}