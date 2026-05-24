using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomItemUI : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI hostNameText;
    public TextMeshProUGUI playerCountText;

    public Button joinButton;

    public void SetRoom(RoomData room)
    {
        string status =
            room.isPrivate
            ? "Private"
            : "Public";

        roomNameText.text =
            $"{room.roomName} ({status})";

        hostNameText.text =
            room.hostName;

        playerCountText.text =
            $"{room.currentPlayers}/" +
            $"{room.maxPlayers}";
    }
}