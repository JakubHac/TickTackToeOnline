using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

[Serializable]
public class RoomData
{
    public readonly ulong RoomID;
    public readonly string HostName;
    public readonly ulong HostID;
    [NonSerialized] public ConcurrentDictionary<int, ClientConnection> Clients = new();
    public readonly byte[][] board = new byte[3][];
    public Dictionary<int, int> playersGems = new ();
    public int turn = -1;
    
    public RoomData(ulong roomID, ClientConnection hostClient)
    {
        RoomID = roomID;
        HostName = hostClient.ClientName;
        HostID = hostClient.ClientID;
        for (int i = 0; i < 3; i++)
        {
            board[i] = new byte[3];
        }
        
        playersGems.Add(0, hostClient.GemIndex);
        while (!Clients.TryAdd(0, hostClient))
        {
            
        }
    }

    public void SignalClientsToStart()
    {
        if (Clients.Count == 2)
        {
            turn = 0;
            foreach (var client in Clients)
            {
                client.Value.SendGameMessage(this);
            }
        }
    }
}
