using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

public class RoomManager
{
    private static ulong nextRoomID = 1;
    private static object roomIDLock = new object();

    public static ulong GetNextRoomID()
    {
        lock (roomIDLock)
        {
            ulong result = nextRoomID;
            if (result == 0)
            {
                result = 1;
                nextRoomID = 1;
            }
            nextRoomID++;
            return result;
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

    public static void HandleClientQuitRoom(ulong clientID)
    {
        List<RoomData> rooms = new();
        foreach (var room in Rooms)
        {
            if (room.Value.Clients.Values.Any(x => x.ClientID == clientID))
            {
                rooms.Add(room.Value);
            }
        }

        foreach (var room in rooms)
        {
            if (room.HostID == clientID)
            {
                if (room.Clients.TryGetValue(1, out var guestClient))
                {
                    guestClient.SendGameWin(room.RoomID);
                }
            }
            else
            {
                if (room.Clients.TryGetValue(0, out var hostClient))
                {
                    hostClient.SendGameWin(room.RoomID);
                }
            }
            Rooms.TryRemove(room.RoomID, out _);
        }
        
    }
}
