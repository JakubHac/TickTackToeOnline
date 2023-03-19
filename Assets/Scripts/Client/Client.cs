using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Doozy.Engine.UI;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;

public class Client : MonoBehaviour
{
	[SerializeField] private TMP_InputField IpAddressInput;
	[SerializeField] private UIView MenuUIView;
	[SerializeField] private UIView ConnectingUIView;
	[SerializeField] private UIView RoomListUIView;
	[SerializeField] private UIView EnteringRoomUIView;
	[SerializeField] private UIView RoomUIView;
	[SerializeField] private Settings Settings;
	
	[SerializeField] private Transform RoomListContent;
	[SerializeField] private GameObject RoomDisplayPrefab;
	[SerializeField] private RoomGameplayController GameplayController;

	TcpClient client;
	public static ConcurrentQueue<ClientToServerMessage> MessagesToSend = new();
	public static ConcurrentQueue<GameObject> ObjectsToDelete = new();
	public static ConcurrentQueue<InstantiateData> ObjectsToInstantiate = new();
	public static ConcurrentQueue<Action> ActionsToExecuteOnMainThread = new();
	
	public ulong ID;

	public void ConnectToServer()
	{
		client ??= new TcpClient();
		
		if (client.Connected) return;
		MessagesToSend.Clear();
		MenuUIView.Hide();
		ConnectingUIView.Show();
		client.ConnectAsync(IpAddressInput.text, 19755).ContinueWith(task =>
		{
			ConnectingUIView.Hide();
			if (task.IsCompleted)
			{
				Debug.Log("Connected to server");
				RoomListUIView.Show();
			}
			else
			{
				Debug.LogError("Failed to connect to server");
				MenuUIView.Show();
			}

			MessagesToSend.Enqueue(ClientToServerMessage.Welcome(Settings));
			BeginServerCommunication();
		});
	}

	private void Update()
	{
		while (ObjectsToDelete.TryDequeue(out var obj))
		{
			obj.SetActive(false);
			Destroy(obj);
		}

		while (ObjectsToInstantiate.TryDequeue(out var instantiateData))
		{
			instantiateData.Execute();
		}
		
		while (ActionsToExecuteOnMainThread.TryDequeue(out var action))
		{
			action.Invoke();
		}
	}

	private async Task BeginServerCommunication()
	{
		var stream = client.GetStream();
		using var sr = new StreamReader(stream, Encoding.UTF8);
		await using var sw = new StreamWriter(stream, Encoding.UTF8);
		double lastPing = Timer.TimeSinceStartup;

		try
		{
			while (true)
			{
				if (lastPing + 10 < Timer.TimeSinceStartup)
				{
					Debug.Log("Server timed out");
					break;
				}

				while (MessagesToSend.Count > 0)
				{
					if (MessagesToSend.TryDequeue(out var message))
					{
						Debug.Log($"Sending message {message.MessageType}");
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
					var serverMessage =
						SerializationUtility.DeserializeValue<ServerToClientMessage>(messageBytes, DataFormat.JSON);
					switch (serverMessage.MessageType)
					{
						case ServerToClientMessageType.WelcomeMessage:
							Debug.Log("Received welcome message");
							ID = ulong.Parse(serverMessage.MessageData);
							break;
						case ServerToClientMessageType.RoomList:
							Debug.Log("Received room list");
							var holderBytes = Encoding.UTF8.GetBytes(serverMessage.MessageData);
							var holder =
								SerializationUtility.DeserializeValue<RoomListHolder>(holderBytes, DataFormat.JSON);
							HandleRoomList(holder);
							break;
						case ServerToClientMessageType.RoomDetails:
							Debug.Log("Received room details");
							var roomBytes = Encoding.UTF8.GetBytes(serverMessage.MessageData);
							var room = SerializationUtility.DeserializeValue<RoomData>(roomBytes, DataFormat.JSON);
							HandleRoomJoin(room);
							break;
						case ServerToClientMessageType.CreateRoomFailure:
						case ServerToClientMessageType.JoinRoomFailure:
							Debug.Log("Failed to create/join room");
							EnteringRoomUIView.Hide();
							RoomUIView.Hide();
							RoomListUIView.Show();
							break;
						case ServerToClientMessageType.Ping:
							lastPing = Timer.TimeSinceStartup;
							MessagesToSend.Enqueue(ClientToServerMessage.Pong());
							break;
						case ServerToClientMessageType.Game:
							Debug.Log("Received Game Update");
							byte[] gameBytes = Encoding.UTF8.GetBytes(serverMessage.MessageData);
							var game = SerializationUtility.DeserializeValue<GameHolder>(gameBytes, DataFormat.JSON);
							HandleGame(game);
							break;
						case ServerToClientMessageType.InstantWin:
							GameplayController.InstantWin(ulong.Parse(serverMessage.MessageData));
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				await Task.Delay(50);
			}
		}
		catch (IOException ioException)
		{
			Debug.Log("IO exception, disconnecting: " + ioException);
		}
		catch (SocketException socketException)
		{
			Debug.Log("Socket exception, disconnecting" + socketException);
		}
		catch (Exception e)
		{
			Debug.LogError(e);
			throw;
		}
		finally
		{
			client?.Close();
			client = null;
		}
	}

	public void DisconnectFromServer()
	{
		client?.Close();
		client = null;
		RoomListUIView.Hide();
		MenuUIView.Show();
	}

	public void RequestRoomList()
	{
		Debug.Log("Requesting room list");
		MessagesToSend.Enqueue(ClientToServerMessage.RoomListRequest(true));
	}

	private void HandleRoomList(RoomListHolder holder)
	{
		Debug.Log(holder.RoomsData.Aggregate("Rooms:", (current, roomData) => current + $"\n{roomData.name}"));
		foreach (var roomDisplay in RoomListContent.GetComponentsInChildren<RoomDisplay>(includeInactive: false))
		{
			ObjectsToDelete.Enqueue(roomDisplay.gameObject);
			// Destroy(roomDisplay.gameObject);
			// roomDisplay.gameObject.SetActive(false);
		}
		
		foreach (var roomData in holder.RoomsData)
		{
			ObjectsToInstantiate.Enqueue(new InstantiateData(RoomDisplayPrefab, Vector3.zero, Quaternion.identity, RoomListContent,
				(x) =>
				{
					var roomDisplay = x.GetComponent<RoomDisplay>();
					roomDisplay.SetRoomData(roomData);
				}));
		}
	}
	
	public void RequestCreateRoom()
	{
		RoomListUIView.Hide();
		EnteringRoomUIView.Show();
		MessagesToSend.Enqueue(ClientToServerMessage.CreateRoomRequest());
	}
	
	private void HandleRoomJoin(RoomData roomData)
	{
		RoomListUIView.Hide();
		EnteringRoomUIView.Hide();
		RoomUIView.Show();
		GameplayController.UpdateState(roomData, true);
	}
	
	private void HandleGame(GameHolder gameHolder)
	{
		RoomListUIView.Hide();
		EnteringRoomUIView.Hide();
		RoomUIView.Show();
		var roomData = gameHolder.RoomData;
		GameplayController.UpdateState(roomData, false);
	}

	public void SendQuitRoom()
	{
		MessagesToSend.Enqueue(ClientToServerMessage.QuitRoom());
	}

	public void SendMove(int x, int y, ulong roomID)
	{
		MessagesToSend.Enqueue(ClientToServerMessage.Move(x, y, roomID));
	}
}