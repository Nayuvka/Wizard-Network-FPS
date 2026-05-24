using System;

[Serializable]
public class RoomData
{
    public string roomName;
    public string hostName;
    public string roomCode;
    public string hostIP;
    public bool isPrivate;
    public int currentPlayers;
    public int maxPlayers;
}