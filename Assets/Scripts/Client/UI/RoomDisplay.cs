using TMPro;
using UnityEngine;

public class RoomDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text RoomNameText;
    private ulong RoomId;

    public void Join()
    {
        Client.MessagesToSend.Enqueue(ClientToServerMessage.JoinRoomRequest(RoomId));
    }
    
    public void SetRoomData((string name, ulong id) roomData)
    {
        RoomId = roomData.id;
        RoomNameText.text = roomData.name;
    }
}
