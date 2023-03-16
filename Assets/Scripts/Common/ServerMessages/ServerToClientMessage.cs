using System.Collections.Generic;
using System.Text;
using Sirenix.Serialization;

[System.Serializable]
public class ServerToClientMessage
{
    public readonly ServerToClientMessageType MessageType;
    public readonly string MessageData;
    
    private ServerToClientMessage(ServerToClientMessageType messageType, string messageData)
    {
        MessageType = messageType;
        MessageData = messageData;
    }
    
    public static ServerToClientMessage WelcomeMessage(ulong clientID)
    {
        return new ServerToClientMessage(ServerToClientMessageType.WelcomeMessage, clientID.ToString());
    }
    
    public static ServerToClientMessage RoomList(List<RoomData> roomList)
    {
        byte[] data = SerializationUtility.SerializeValue(new RoomListHolder(roomList), DataFormat.JSON);
        return new ServerToClientMessage(ServerToClientMessageType.RoomList, Encoding.UTF8.GetString(data));
    }
    
    public static ServerToClientMessage RoomDetails(ulong roomID)
    {
        byte[] data = SerializationUtility.SerializeValue(RoomManager.GetRoomDetails(roomID), DataFormat.JSON);
        return new ServerToClientMessage(ServerToClientMessageType.RoomDetails, Encoding.UTF8.GetString(data));
    }

    private static readonly ServerToClientMessage CreateRoomFailureMessage = new(ServerToClientMessageType.CreateRoomFailure, string.Empty);
    
    public static ServerToClientMessage CreateRoomFailure()
    {
        return CreateRoomFailureMessage;
    }

    private static readonly ServerToClientMessage PingMessage = new(ServerToClientMessageType.Ping, string.Empty);
    
    public static ServerToClientMessage Ping()
    {
        return PingMessage;
    }
    
    private static readonly ServerToClientMessage JoinRoomFailureMessage = new(ServerToClientMessageType.JoinRoomFailure, string.Empty);

    public static ServerToClientMessage JoinRoomFailure()
    {
        return JoinRoomFailureMessage;
    }

    public static ServerToClientMessage SendGame(RoomData roomData)
    {
        byte[] data = SerializationUtility.SerializeValue(new GameHolder(roomData), DataFormat.JSON);
        string messageData = Encoding.UTF8.GetString(data);
        return new ServerToClientMessage(ServerToClientMessageType.Game, messageData);
    }
}
