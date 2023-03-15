using System;
using System.Collections.Concurrent;
using System.IO;
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
	[SerializeField] private Settings Settings;
	
	[SerializeField] private Transform RoomListContent;
	[SerializeField] private GameObject RoomDisplayPrefab;

	TcpClient client = new TcpClient();
	public static ConcurrentQueue<ClientToServerMessage> MessagesToSend = new();
	private ulong ID;

	public void ConnectToServer()
	{
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

	private async Task BeginServerCommunication()
	{
		var stream = client.GetStream();
		using var sr = new StreamReader(stream, Encoding.UTF8);
		await using var sw = new StreamWriter(stream, Encoding.UTF8);
		double lastPing = Timer.TimeSinceStartup;

		try
		{
			while (client.Connected)
			{
				if (lastPing + 10 < Timer.TimeSinceStartup)
				{
					client.Close();
					Debug.Log("Server timed out");
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
					var serverMessage =
						SerializationUtility.DeserializeValue<ServerToClientMessage>(messageBytes, DataFormat.JSON);
					switch (serverMessage.MessageType)
					{
						case ServerToClientMessageType.WelcomeMessage:
							ID = ulong.Parse(serverMessage.MessageData);
							break;
						case ServerToClientMessageType.RoomList:
							var holderBytes = Encoding.UTF8.GetBytes(serverMessage.MessageData);
							var holder =
								SerializationUtility.DeserializeValue<RoomListHolder>(holderBytes, DataFormat.JSON);
							HandleRoomList(holder);
							break;
						case ServerToClientMessageType.RoomDetails:
							break;
						case ServerToClientMessageType.CreateRoomFailure:
							break;
						case ServerToClientMessageType.Ping:
							MessagesToSend.Enqueue(ClientToServerMessage.Pong());
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
			Debug.Log("IO exception, disconnecting");
		}
		catch (SocketException socketException)
		{
			Debug.Log("Socket exception, disconnecting");
		}
		catch (Exception e)
		{
			Debug.LogError(e);
			throw;
		}
		finally
		{
			client?.Close();
		}
	}

	public void DisconnectFromServer()
	{
		client.Close();
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
		foreach (var roomDisplay in RoomListContent.GetComponentsInChildren<RoomDisplay>(includeInactive: false))
		{
			Destroy(roomDisplay.gameObject);
			roomDisplay.gameObject.SetActive(false);
		}

		foreach (var roomData in holder.RoomsData)
		{
			var roomDisplay = Instantiate(RoomDisplayPrefab, RoomListContent).GetComponent<RoomDisplay>();
			roomDisplay.SetRoomData(roomData);
		}
	}
}