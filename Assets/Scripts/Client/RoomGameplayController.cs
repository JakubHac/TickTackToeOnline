using UnityEngine;

public class RoomGameplayController : MonoBehaviour
{
	[SerializeField] private Transform BoardGrid;
	[SerializeField] private GemsCollection GemsCollection;
	[SerializeField] private Settings Settings;
	
	
	
	private GameObject[] GridButtons;
	
	private void Start()
	{
		GridButtons = new GameObject[BoardGrid.childCount];
		for (int i = 0; i < BoardGrid.childCount; i++)
		{
			GridButtons[i] = BoardGrid.GetChild(i).gameObject;
		}
	}

	public void WaitState()
	{
		foreach (var gridButton in GridButtons)
		{
			ImageAnimator animator = gridButton.GetComponentInChildren<ImageAnimator>();
			animator.SetAnimation(GemsCollection.Gems[Settings.SelectedGem]);
		}
	}
	
	public void DisplayState(GameHolder game)
	{
		for (int i = 0; i < GridButtons.Length; i++)
		{
			var button = GridButtons[i];
		}
	}
	
	public void ClickedGridButton(int getSiblingIndex)
	{
		
	}
}
