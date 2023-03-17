using System.Text;
using Sirenix.Serialization;

[System.Serializable]
public class ClientToServerMessage
{
    public readonly ClientToServerMessageType MessageType;
    public readonly string MessageData;
    
    private ClientToServerMessage(ClientToServerMessageType messageType, string messageData)
    {
        MessageType = messageType;
        MessageData = messageData;
    }
    
    public static ClientToServerMessage Welcome(Settings settings)
    {
        byte[] data = SerializationUtility.SerializeValue(new ClientWelcomeMessageData(settings.selectedNickname, settings.SelectedGem), DataFormat.JSON);
        return new ClientToServerMessage(ClientToServerMessageType.WelcomeMessage, Encoding.UTF8.GetString(data));
    }
    
    public static ClientToServerMessage RoomListRequest(bool nonFull)
    {
        return new ClientToServerMessage(ClientToServerMessageType.RoomListRequest, nonFull ? "NonFull" : "All");
    }
    
    public static ClientToServerMessage CreateRoomRequest()
    {
        return new ClientToServerMessage(ClientToServerMessageType.CreateRoomRequest, string.Empty);
    }
    
    public static ClientToServerMessage JoinRoomRequest(ulong roomID)
    {
        return new ClientToServerMessage(ClientToServerMessageType.JoinRoomRequest, roomID.ToString());
    }

    private static readonly ClientToServerMessage PongMessage = new(ClientToServerMessageType.Pong, string.Empty);
    
    public static ClientToServerMessage Pong()
    {
        return PongMessage;
    }

    public static ClientToServerMessage Move(int x, int y, ulong roomID)
    {
        var move = new MoveHolder(x, y, roomID);
        byte[] data = SerializationUtility.SerializeValue(move, DataFormat.JSON);
        return new ClientToServerMessage(ClientToServerMessageType.Move, Encoding.UTF8.GetString(data));
    }
}
