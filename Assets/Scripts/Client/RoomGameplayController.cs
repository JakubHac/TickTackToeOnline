using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomGameplayController : MonoBehaviour
{
	[SerializeField] private Transform BoardGrid;
	[SerializeField] private GemsCollection GemsCollection;
	[SerializeField] private Settings Settings;
	[SerializeField] private Material OurGemsMaterial;
	[SerializeField] private Material EnemyGemsMaterial;
	[SerializeField] private TMP_Text StateText;
	[SerializeField] private Client Client;
	[SerializeField] private GameObject QuitRoomButton;
	
	private BoardState CurrentState = BoardState.NotResolved;
	
	private GameObject[] GridButtons;
	RoomData lastRoomData;

	private ulong roomID;
	
	private void Start()
	{
		GridButtons = new GameObject[BoardGrid.childCount];
		for (int i = 0; i < BoardGrid.childCount; i++)
		{
			GridButtons[i] = BoardGrid.GetChild(i).gameObject;
		}
	}

	private void WaitState()
	{
		Client.ActionsToExecuteOnMainThread.Enqueue(() =>
		{
			StateText.text = "Oczekiwanie na drugiego gracza";
			QuitRoomButton.SetActive(true);
			foreach (var gridButton in GridButtons)
			{
				ImageAnimator animator = gridButton.GetComponentInChildren<ImageAnimator>();
				animator.SetAnimation(GemsCollection.Gems[Settings.SelectedGem]);
				animator.enabled = true;
				animator.SetToFirstFrame();
				animator.AllowedLoops = -1;
				Image image = animator.GetComponentInChildren<Image>();
				image.enabled = true;
				image.material = OurGemsMaterial;
				gridButton.GetComponentInChildren<Button>().interactable = false;
			}
		});
	}

	private void NotOurTurnState(PerspectiveBoardState boardState)
	{
		Client.ActionsToExecuteOnMainThread.Enqueue(() =>
		{
			StateText.text = "Ruch drugiego gracza";
			QuitRoomButton.SetActive(false);
		});
		AssignGemsToGrid(boardState);
	}

	public void InstantWin(ulong roomID)
	{
		if (this.roomID != roomID) return;
		var boardState = new PerspectiveBoardState(lastRoomData, Client, true);
		WeWonState(boardState);
	}
	
	public void UpdateState(RoomData room, bool join)
	{
		if (join)
		{
			Debug.Log($"Joined room {room.RoomID}");
			roomID = room.RoomID;
		}
		else if (roomID != room.RoomID)
		{
			Debug.Log($"Room id mismatch, expected {roomID} got {room.RoomID}");
			return;
		}
		
		lastRoomData = room;
		var boardState = new PerspectiveBoardState(room, Client);
		CurrentState = boardState.State;
		Debug.Log($"Entered state: {boardState.State}");
		switch (boardState.State)
		{
			case BoardState.WaitingForPlayer:
				WaitState();
				break;
			case BoardState.EnemyTurn:
				NotOurTurnState(boardState);
				break;
			case BoardState.OurTurn:
				OurTurnState(boardState);
				break;
			case BoardState.WeWon:
				WeWonState(boardState);
				break;
			case BoardState.EnemyWon:
				EnemyWonState(boardState);
				break;
			case BoardState.NotResolved:
				NotResolvedState(boardState);
				break;
		}
	}

	private void NotResolvedState(PerspectiveBoardState boardState)
	{ 
		Client.ActionsToExecuteOnMainThread.Enqueue(() =>
		{
			StateText.text = "Coś się zepsuło";
			QuitRoomButton.SetActive(true);
		});
		AssignGemsToGrid(boardState);
	}

	private void EnemyWonState(PerspectiveBoardState boardState)
	{
		Client.ActionsToExecuteOnMainThread.Enqueue(() =>
		{
			StateText.text = "Porażka";
			QuitRoomButton.SetActive(true);
		});
		AssignGemsToGrid(boardState);
		roomID = 0;
	}

	private void WeWonState(PerspectiveBoardState boardState)
	{
		Client.ActionsToExecuteOnMainThread.Enqueue(() =>
		{
			StateText.text = "Zwycięstwo!";
			QuitRoomButton.SetActive(true);
		});
		AssignGemsToGrid(boardState);
		roomID = 0;
	}

	private void OurTurnState(PerspectiveBoardState boardState)
	{
		Client.ActionsToExecuteOnMainThread.Enqueue(() =>
		{
			StateText.text = "Twój ruch";
			QuitRoomButton.SetActive(false);
		});
		AssignGemsToGrid(boardState);
	}

	public void AssignGemsToGrid(PerspectiveBoardState boardState)
	{
		Client.ActionsToExecuteOnMainThread.Enqueue(() =>
		{
			for (int i = 0; i < GridButtons.Length; i++)
			{
				int x = i % 3;
				int y = i / 3;
				ulong fieldOwner = boardState.Game.board[x][y];
				Debug.Log($"Field [{i}] owner: {fieldOwner}");
				var gridButton = GridButtons[i];
				var animator = gridButton.GetComponentInChildren<ImageAnimator>();
				var gemImage = animator.GetComponentInChildren<Image>();
				var button = gridButton.GetComponentInChildren<Button>();
				if (fieldOwner == 0)
				{
					gemImage.enabled = false;
					animator.enabled = false;
					button.interactable = boardState.State == BoardState.OurTurn;
				}
				else if (fieldOwner == Client.ID)
				{
					animator.enabled = true;
					animator.SetAnimation(GemsCollection.Gems[Settings.SelectedGem]);
					if (boardState.State == BoardState.WeWon)
					{
						animator.AllowedLoops = -1;
						animator.SetToFirstFrame();
					}
					else
					{
						animator.AllowedLoops = 0;
						animator.SetToFirstFrame();
					}
					gemImage.enabled = true;
					gemImage.material = OurGemsMaterial;
					button.interactable = false;
				}
				else
				{
					animator.enabled = true;
					animator.SetAnimation(GemsCollection.Gems[boardState.GetEnemyGemIndex()]);
					if (boardState.State == BoardState.EnemyWon)
					{
						animator.AllowedLoops = -1;
						animator.SetToFirstFrame();
					}
					else
					{
						animator.AllowedLoops = 0;
						animator.SetToFirstFrame();
					}
					gemImage.enabled = true;
					gemImage.material = EnemyGemsMaterial;
					button.interactable = false;
				}
			}
		});
	}
	
	public void ClickedGridButton(int getSiblingIndex)
	{
		if (CurrentState != BoardState.OurTurn) return;
		
		int x = getSiblingIndex % 3;
		int y = getSiblingIndex / 3;
		Client.SendMove(x, y, roomID);
		
		foreach (var gridButton in GridButtons)
		{
			gridButton.GetComponentInChildren<Button>().interactable = false;
		}
	}
	
	public void QuitRoom()
	{
		Client.SendQuitRoom();
		roomID = 0;
	}
}
