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
	private static ulong nextClientID = 1;
	private static object clientIDLock = new object();

	public static ulong GetNextClientID()
	{
		lock (clientIDLock)
		{
			var result = nextClientID;
			if (result == 0)
			{
				result = 1;
				nextClientID = 2;
			}
			else
			{
				nextClientID++;
			}
			return result;
		}
	}

	private TcpClient _client;
	public readonly ulong ClientID;
	public string ClientName;
	public int GemIndex;

	public ConcurrentQueue<ServerToClientMessage> MessagesToSend = new ConcurrentQueue<ServerToClientMessage>();

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
						Debug.Log($"Sending message to client {ClientID}[{ClientName}] - {message.MessageType}");
						byte[] messageBytes = SerializationUtility.SerializeValue(message, DataFormat.JSON);
						string json = Encoding.UTF8.GetString(messageBytes).Replace(Environment.NewLine, "");
						await sw.WriteLineAsync(json);
						await sw.FlushAsync();
					}
				}

				while (stream.DataAvailable)
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
								roomList.RemoveAll(x => x.Clients.Count >= 2);
							}
							MessagesToSend.Enqueue(ServerToClientMessage.RoomList(roomList));
							break;
						case ClientToServerMessageType.CreateRoomRequest:
							Debug.Log($"Client {ClientID}[{ClientName}] requested to create room");
							var createdRoom = RoomManager.CreateRoom(this);
							MessagesToSend.Enqueue(createdRoom != null
								? ServerToClientMessage.RoomDetails(createdRoom.RoomID)
								: ServerToClientMessage.CreateRoomFailure());
							break;
						case ClientToServerMessageType.JoinRoomRequest:
							ulong roomID = ulong.Parse(message.MessageData);
							var roomToJoin = RoomManager.GetRoomDetails(roomID);
							if (roomToJoin == null)
							{
								Debug.Log($"Client {ClientID}[{ClientName}] requested to join non-existent room [{roomID}]");
								MessagesToSend.Enqueue(ServerToClientMessage.JoinRoomFailure());
							}
							else if (roomToJoin.Clients.Count == 2)
							{
								Debug.Log($"Client {ClientID}[{ClientName}] tried to join full room");
								MessagesToSend.Enqueue(ServerToClientMessage.JoinRoomFailure());
							}
							else
							{
								Debug.Log($"Client {ClientID}[{ClientName}] joined room {roomID}");

								bool joinSuccess = roomToJoin.Clients.TryAdd(1, this);
								if (joinSuccess)
								{
									roomToJoin.playersGems.Add(ClientID, GemIndex);
									MessagesToSend.Enqueue(ServerToClientMessage.RoomDetails(roomID));
									roomToJoin.SendGameDataToClients();
								}
								else
								{
									MessagesToSend.Enqueue(ServerToClientMessage.JoinRoomFailure());
								}
							}
							break;
						case ClientToServerMessageType.Move:
							byte[] moveDataBytes = Encoding.UTF8.GetBytes(message.MessageData);
							var moveData = SerializationUtility.DeserializeValue<MoveHolder>(moveDataBytes, DataFormat.JSON);
							var room = RoomManager.GetRoomDetails(moveData.RoomID);
							if (room != null)
							{
								room.HandleMove(moveData, ClientID);
							}
							break;
						case ClientToServerMessageType.WelcomeMessage:
							byte[] messageDataBytes = Encoding.UTF8.GetBytes(message.MessageData);
							ClientWelcomeMessageData welcomeMessageData =
								SerializationUtility.DeserializeValue<ClientWelcomeMessageData>(messageDataBytes,
									DataFormat.JSON);
							ClientName = welcomeMessageData.ClientName;
							GemIndex = welcomeMessageData.GemIndex;
							Debug.Log($"Client {ClientID} is now known as {ClientName}");
							break;
						case ClientToServerMessageType.Pong:
							Debug.Log($"Received pong from {ClientID}[{ClientName}]");
							lastPong = Timer.TimeSinceStartup;
							break;
						case ClientToServerMessageType.QuitRoom:
							RoomManager.HandleClientQuitRoom(ClientID);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				await Task.Delay(50);
			}
		}
		catch(IOException ioException)
		{
			Debug.Log($"IO exception on Client {ClientID}[{ClientName}], disconnecting: {ioException}");
		}
		catch (SocketException socketException)
		{
			Debug.Log($"Socket exception on Client {ClientID}[{ClientName}], disconnecting: {socketException}");
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
		finally
		{
			Debug.Log($"Client {ClientID}[{ClientName}] connection closed");
			RoomManager.HandleClientQuitRoom(ClientID);
			if (_client != null)
			{
				(_client as IDisposable).Dispose();
				_client = null;
			}
		}
	}

	public void SendGameMessage(RoomData roomData)
	{
		MessagesToSend.Enqueue(ServerToClientMessage.SendGame(roomData));
	}

	
	public void SendGameWin(ulong roomId)
	{
		MessagesToSend.Enqueue(ServerToClientMessage.InstantWin(roomId));
	}
}