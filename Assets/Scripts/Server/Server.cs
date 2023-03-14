using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
            Task.Run((Func<Task>)cw.DoSomethingWithClientAsync);
        }
    }

    class ClientConnection
    {
        TcpClient _client;

        public ClientConnection(TcpClient client)
        {
            _client = client;
        }

        public async Task DoSomethingWithClientAsync()
        {
            try
            {
                using (var stream = _client.GetStream())
                {
                    using (var sr = new StreamReader(stream))
                    using (var sw = new StreamWriter(stream))
                    {
                        await sw.WriteLineAsync("Server ready").ConfigureAwait(false);
                        await sw.FlushAsync().ConfigureAwait(false);
                        string data = string.Empty;
                        while (!((data = await sr.ReadLineAsync().ConfigureAwait(false)).Equals("exit", StringComparison.OrdinalIgnoreCase)))
                        {

                            Thread.Sleep(10);
                            // await sw.WriteLineAsync(data).ConfigureAwait(false);
                            // await sw.FlushAsync().ConfigureAwait(false);
                        }
                    }

                }
            }
            finally
            {
                if (_client != null)
                {
                    (_client as IDisposable).Dispose();
                    _client = null;
                }
            }
        }
    }
}
