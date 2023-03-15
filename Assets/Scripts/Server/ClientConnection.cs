using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Sirenix.Serialization;
using UnityEngine;

public class ClientConnection
{
	private static ulong nextClientID = 0;
	private static object clientIDLock = new object();

	public static ulong GetNextClientID()
	{
		lock (clientIDLock)
		{
			return nextClientID++;
		}
	}

	private TcpClient _client;
	public readonly ulong ClientID;
	public string ClientName;
	public int GemIndex;
	
	public ConcurrentQueue<ServerToClientMessage> MessagesToSend = new ConcurrentQueue<ServerToClientMessage>();

	public bool Valid()
	{
		return _client is { Connected: true };
	}

	public ClientConnection(TcpClient client)
	{
		_client = client;
		ClientID = GetNextClientID();
		Debug.Log($"New client connected: {ClientID}");
		MessagesToSend.Enqueue(ServerToClientMessage.WelcomeMessage(ClientID));
	}

	public async Task HandleCommunicationWithClientAsync()
	{
		try
		{
			double lastPing = Timer.TimeSinceStartup;
			double lastPong = Timer.TimeSinceStartup;
			await using var stream = _client.GetStream();
			using var sr = new StreamReader(stream, Encoding.UTF8);
			await using var sw = new StreamWriter(stream, Encoding.UTF8);
			while (true)
			{
				if (lastPing + 5 < Timer.TimeSinceStartup)
				{
					lastPing = Timer.TimeSinceStartup;
					MessagesToSend.Enqueue(ServerToClientMessage.Ping());
				}

				if (lastPong + 10 < Timer.TimeSinceStartup)
				{
					Debug.Log($"Client {ClientID} timed out");
					break;
				}

				while (MessagesToSend.Count > 0)
				{
					if (MessagesToSend.TryDequeue(out var message))
					{
						byte[] messageBytes = SerializationUtility.SerializeValue(message, DataFormat.JSON);
						string json = Encoding.UTF8.GetString(messageBytes).Replace(Environment.NewLine, "");
						await sw.WriteLineAsync(json);
						await sw.FlushAsync();
					}
				}

				while (stream.CanRead)
				{
					string json = await sr.ReadLineAsync();
					if (json == null)
					{
						break;
					}

					byte[] messageBytes = Encoding.UTF8.GetBytes(json);
					var message =
						SerializationUtility.DeserializeValue<ClientToServerMessage>(messageBytes, DataFormat.JSON);
					switch (message.MessageType)
					{
						case ClientToServerMessageType.RoomListRequest:
							Debug.Log($"Client {ClientID}[{ClientName}] requested room list");
							var roomList = RoomManager.GetRoomList();
							if (message.MessageData == "NonFull")
							{
								roomList.RemoveAll(x => x.Clients.Count < 2);
							}

							MessagesToSend.Enqueue(ServerToClientMessage.RoomList(roomList));
							break;
						case ClientToServerMessageType.CreateRoomRequest:
							var room = RoomManager.CreateRoom(this);
							MessagesToSend.Enqueue(room != null
								? ServerToClientMessage.RoomDetails(room.RoomID)
								: ServerToClientMessage.CreateRoomFailure());
							break;
						case ClientToServerMessageType.JoinRoomRequest:
							break;
						case ClientToServerMessageType.Move:
							break;
						case ClientToServerMessageType.WelcomeMessage:
							byte[] messageDataBytes = Encoding.UTF8.GetBytes(message.MessageData);
							ClientWelcomeMessageData welcomeMessageData = SerializationUtility.DeserializeValue<ClientWelcomeMessageData>(messageDataBytes, DataFormat.JSON);
							ClientName = welcomeMessageData.ClientName;
							GemIndex = welcomeMessageData.GemIndex;
							Debug.Log($"Client {ClientID} is now known as {ClientName}");
							break;
						case ClientToServerMessageType.Pong:
							lastPong = Timer.TimeSinceStartup;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				await Task.Delay(50);
			}
		}
		catch (SocketException socketException)
		{
			Debug.Log($"Socket exception on Client {ClientID}[{ClientName}], disconnecting");
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
		finally
		{
			Debug.Log($"Client {ClientID}[{ClientName}] connection closed");
			if (_client != null)
			{
				(_client as IDisposable).Dispose();
				_client = null;
			}
		}
	}
}