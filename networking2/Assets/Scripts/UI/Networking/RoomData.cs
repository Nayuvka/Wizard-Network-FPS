using System;

[Serializable]
public class RoomData
{
    public string roomName;
    public string roomCode;
    public string hostIP;

    public int currentPlayers;
    public int maxPlayers;
}