using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;

public class RoomManager
{
    private static ulong nextRoomID = 0;
    private static object roomIDLock = new object();

    public static ulong GetNextRoomID()
    {
        lock (roomIDLock)
        {
            return nextRoomID++;
        }
    }
    
    private static ConcurrentDictionary<ulong, RoomData> Rooms = new();
    
    public static List<RoomData> GetRoomList()
    {
        List<RoomData> roomList = new();
        foreach (var room in Rooms)
        {
            roomList.Add(room.Value);
        }

        return roomList;
    }

    [CanBeNull]
    public static RoomData CreateRoom(ClientConnection hostClient)
    {
        ulong roomID = GetNextRoomID();
        if (Rooms.TryAdd(roomID, new RoomData(roomID, hostClient)))
        {
            return Rooms[roomID];
        }

        return null;
    }

    [CanBeNull]
    public static RoomData GetRoomDetails(ulong roomID)
    {
        if (Rooms.TryGetValue(roomID, out var roomData))
        {
            return roomData;
        }
        return null;
    }
}
