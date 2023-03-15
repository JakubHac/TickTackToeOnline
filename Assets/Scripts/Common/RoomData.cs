using System;
using System.Collections.Concurrent;

[Serializable]
public class RoomData
{
    public readonly ulong RoomID;
    public readonly string HostName;
    [NonSerialized] public ConcurrentDictionary<int, ClientConnection> Clients = new();
    public readonly byte[][] board = new byte[3][];
    
    public RoomData(ulong roomID, ClientConnection hostClient)
    {
        RoomID = roomID;
        HostName = hostClient.ClientName;
        for (int i = 0; i < 3; i++)
        {
            board[i] = new byte[3];
        }
        
        while (!Clients.TryAdd(0, hostClient))
        {
            
        }
    }
}
