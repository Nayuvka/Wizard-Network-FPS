using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class LanDiscovery : MonoBehaviour
{
    public static LanDiscovery Instance;

    [SerializeField] private int discoveryPort = 47777;

    private UdpClient udpBroadcaster;
    private UdpClient udpListener;
    private bool isHosting;
    private bool isSearching;

    public event Action<RoomData> OnRoomFound;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void StartBroadcasting(string roomName, string roomCode, ushort gamePort)
    {
        StopBroadcasting();
        isHosting = true;

        RoomData data = new RoomData
        {
            roomName = roomName,
            roomCode = roomCode,
            hostIP = GetLocalIPAddress(),
            currentPlayers = 1,
            maxPlayers = 4
        };

        string json = JsonUtility.ToJson(data);
        udpBroadcaster = new UdpClient { EnableBroadcast = true };

        Task.Run(() => BroadcastLoop(json, gamePort));
    }

    private async Task BroadcastLoop(string jsonPayload, ushort port)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(jsonPayload);
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, discoveryPort);

        while (isHosting)
        {
            try
            {
                await udpBroadcaster.SendAsync(bytes, bytes.Length, endPoint);
                await Task.Delay(1500); // Broadcast every 1.5 seconds
            }
            catch { break; }
        }
    }

    public void StartSearching()
    {
        StopSearching();
        isSearching = true;
        udpListener = new UdpClient(discoveryPort);
        Task.Run(ListenLoop);
    }

    private async Task ListenLoop()
    {
        while (isSearching)
        {
            try
            {
                UdpReceiveResult result = await udpListener.ReceiveAsync();
                string json = Encoding.UTF8.GetString(result.Buffer);
                RoomData room = JsonUtility.FromJson<RoomData>(json);

                if (room != null)
                {
                    // Ensure local UI code runs on the Unity main thread
                    UnityMainThreadDispatcher.ExecuteOnMainThread(() => {
                        OnRoomFound?.Invoke(room);
                    });
                }
            }
            catch { break; }
        }
    }

    public void StopBroadcasting() { isHosting = false; udpBroadcaster?.Close(); }
    public void StopSearching() { isSearching = false; udpListener?.Close(); }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
        }
        return "127.0.0.1";
    }

    private void OnDestroy() { StopBroadcasting(); StopSearching(); }
}
