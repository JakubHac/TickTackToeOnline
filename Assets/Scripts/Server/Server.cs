using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class Server : MonoBehaviour
{
    void Start()
    {
        ListenForClients();
    }

    async Task ListenForClients()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 19755);
        server.Start();
        Debug.Log("Server started");
        while (true)
        {
            var client = await server.AcceptTcpClientAsync().ConfigureAwait(false);
            var cw = new ClientConnection(client);
            Task.Run((Func<Task>)cw.HandleCommunicationWithClientAsync);
        }
    }
    
}
