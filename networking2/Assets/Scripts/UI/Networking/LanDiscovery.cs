using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class LanDiscovery : MonoBehaviour
{
    public static LanDiscovery Instance;

    [Header("Discovery")]
    [SerializeField] private int discoveryPort = 47777;

    [SerializeField] private float broadcastInterval = 1.5f;

    private UdpClient udpBroadcaster;

    private UdpClient udpListener;

    private bool isHosting;

    private bool isSearching;

    public event Action<RoomData> OnRoomFound;

    private RoomData currentRoomData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsRoomFull()
    {
        if (currentRoomData == null)
            return false;

        return currentRoomData.currentPlayers >=
               currentRoomData.maxPlayers;
    }

    #region HOSTING

    public void StartBroadcasting(
        RoomData roomData)
    {
        StopBroadcasting();

        isHosting = true;

        currentRoomData = roomData;

        udpBroadcaster = new UdpClient();

        udpBroadcaster.EnableBroadcast = true;

        Task.Run(BroadcastLoop);
    }

    private async Task BroadcastLoop()
    {
        IPEndPoint endPoint =
            new IPEndPoint(
                IPAddress.Broadcast,
                discoveryPort);

        while (isHosting)
        {
            try
            {
                string json =
                    JsonUtility.ToJson(
                        currentRoomData);

                byte[] bytes =
                    Encoding.UTF8.GetBytes(json);

                await udpBroadcaster.SendAsync(
                    bytes,
                    bytes.Length,
                    endPoint);

                await Task.Delay(
                    Mathf.RoundToInt(
                        broadcastInterval * 1000));
            }
            catch
            {
                break;
            }
        }
    }

    public void UpdatePlayerCount(int currentPlayers)
    {
        if (currentRoomData == null)
            return;

        currentRoomData.currentPlayers =
            currentPlayers;
    }

    public void StopBroadcasting()
    {
        isHosting = false;

        udpBroadcaster?.Close();

        udpBroadcaster = null;
    }

    #endregion

    #region SEARCHING

    public void StartSearching()
    {
        if (isSearching)
            return;

        StopSearching();

        isSearching = true;

        udpListener = new UdpClient();

        udpListener.EnableBroadcast = true;

        udpListener.Client.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.ReuseAddress,
            true);

        udpListener.Client.Bind(
            new IPEndPoint(
                IPAddress.Any,
                discoveryPort));

        Task.Run(ListenLoop);
    }

    private async Task ListenLoop()
    {
        while (isSearching)
        {
            try
            {
                UdpReceiveResult result =
                    await udpListener.ReceiveAsync();

                string json =
                    Encoding.UTF8.GetString(
                        result.Buffer);

                RoomData room =
                    JsonUtility.FromJson<RoomData>(
                        json);

                if (room != null)
                {
                    UnityMainThreadDispatcher
                        .ExecuteOnMainThread(() =>
                        {
                            OnRoomFound?.Invoke(room);
                        });
                }
            }
            catch
            {
                break;
            }
        }
    }

    public void StopSearching()
    {
        isSearching = false;

        udpListener?.Close();

        udpListener = null;
    }

    #endregion

    public string GetLocalIPAddress()
    {
        var host =
            Dns.GetHostEntry(
                Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily ==
                AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        return "127.0.0.1";
    }

    private void OnDestroy()
    {
        StopBroadcasting();

        StopSearching();
    }
}