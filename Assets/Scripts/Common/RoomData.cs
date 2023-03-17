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
    public readonly ulong[][] board = new ulong[3][];
    public Dictionary<ulong, int> playersGems = new ();
    public int turn = -1;
    
    public RoomData(ulong roomID, ClientConnection hostClient)
    {
        RoomID = roomID;
        HostName = hostClient.ClientName;
        HostID = hostClient.ClientID;
        for (int i = 0; i < 3; i++)
        {
            board[i] = new ulong[3];
        }
        
        playersGems.Add(hostClient.ClientID, hostClient.GemIndex);
        while (!Clients.TryAdd(0, hostClient))
        {
            
        }
    }

    public void SendGameDataToClients()
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

    public void HandleMove(MoveHolder moveData, ulong sender)
    {
        if (turn == 0 && sender == HostID)
        {
            board[moveData.x][moveData.y] = sender;
            turn = 1;
        }
        else if (turn == 1 && sender != HostID)
        {
            board[moveData.x][moveData.y] = sender;
            turn = 0;
        }
        
        foreach (var client in Clients)
        {
            client.Value.SendGameMessage(this);
        }
    }
}
