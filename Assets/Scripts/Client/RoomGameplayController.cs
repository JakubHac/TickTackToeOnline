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
	
	private BoardState LastState = BoardState.NotResolved;
	
	private GameObject[] GridButtons;

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
		StateText.text = "Oczekiwanie na drugiego gracza";
		QuitRoomButton.SetActive(true);
		foreach (var gridButton in GridButtons)
		{
			ImageAnimator animator = gridButton.GetComponentInChildren<ImageAnimator>();
			animator.SetAnimation(GemsCollection.Gems[Settings.SelectedGem]);
			Image image = gridButton.GetComponentInChildren<Image>();
			image.enabled = true;
			image.material = OurGemsMaterial;
			gridButton.GetComponentInChildren<Button>().interactable = false;
		}
	}

	private void NotOurTurnState(PerspectiveBoardState boardState)
	{
		QuitRoomButton.SetActive(false);
		AssignGemsToGrid(boardState);
		StateText.text = "Ruch drugiego gracza";
	}
	
	public void UpdateState(RoomData room, bool join)
	{
		if (join)
		{
			roomID = room.RoomID;
		}
		else
		{
			if (roomID != room.RoomID)
			{
				return;
			}
		}
		
		var boardState = new PerspectiveBoardState(room, Client);
		LastState = boardState.State;
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
		QuitRoomButton.SetActive(true);
		AssignGemsToGrid(boardState);
		StateText.text = "Coś się zepsuło";
	}

	private void EnemyWonState(PerspectiveBoardState boardState)
	{
		QuitRoomButton.SetActive(true);
		AssignGemsToGrid(boardState);
		StateText.text = "Porażka";
	}

	private void WeWonState(PerspectiveBoardState boardState)
	{
		QuitRoomButton.SetActive(true);
		AssignGemsToGrid(boardState);
		StateText.text = "Zwycięstwo!";
	}

	private void OurTurnState(PerspectiveBoardState boardState)
	{
		QuitRoomButton.SetActive(false);
		AssignGemsToGrid(boardState);
		StateText.text = "Twój ruch";
	}

	public void AssignGemsToGrid(PerspectiveBoardState boardState)
	{
		for (int i = 0; i < GridButtons.Length; i++)
		{
			int x = i % 3;
			int y = i / 3;
			ulong fieldOwner = boardState.Game.board[x][y];
			var gridButton = GridButtons[i];
			if (fieldOwner == 0)
			{
				gridButton.GetComponentInChildren<Image>().enabled = false;
				gridButton.GetComponentInChildren<ImageAnimator>().enabled = false;
				gridButton.GetComponentInChildren<Button>().interactable = boardState.State == BoardState.OurTurn;
			}
			else if (fieldOwner == Client.ID)
			{
				ImageAnimator animator = gridButton.GetComponentInChildren<ImageAnimator>();
				animator.AllowedLoops = boardState.State == BoardState.WeWon ? -1 : 0;
				animator.SetAnimation(GemsCollection.Gems[Settings.SelectedGem]);
				Image image = gridButton.GetComponentInChildren<Image>();
				image.enabled = true;
				image.material = OurGemsMaterial;
				gridButton.GetComponentInChildren<Button>().interactable = false;
			}
			else
			{
				ImageAnimator animator = gridButton.GetComponentInChildren<ImageAnimator>();
				animator.AllowedLoops = boardState.State == BoardState.EnemyWon ? -1 : 0;
				animator.SetAnimation(GemsCollection.Gems[boardState.GetEnemyGemIndex()]);
				Image image = gridButton.GetComponentInChildren<Image>();
				image.enabled = true;
				image.material = EnemyGemsMaterial;
				gridButton.GetComponentInChildren<Button>().interactable = false;
			}
		}
	}
	
	public void ClickedGridButton(int getSiblingIndex)
	{
		if (LastState != BoardState.OurTurn) return;
		
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
		roomID = 0;
	}
}
