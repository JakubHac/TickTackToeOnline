using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Settings : MonoBehaviour
{
	[SerializeField] private GemsCollection GemsCollection;

	[SerializeField] private TMP_InputField NicknameInput;
	[SerializeField] private ImageAnimator GemAnimator;
	
	[NonSerialized] public int SelectedGem = 0;
	[NonSerialized] public string selectedNickname = "Gracz";

	private void Start()
	{
		SelectedGem = PlayerPrefs.HasKey("SelectedGem") ? PlayerPrefs.GetInt("SelectedGem") : 0;
		selectedNickname = PlayerPrefs.HasKey("SelectedNickname") ? PlayerPrefs.GetString("SelectedNickname") : "Gracz";
	}
	
	public void SetupSettings()
	{
		NicknameInput.text = selectedNickname;
		GemAnimator.SetAnimation(GemsCollection.Gems[SelectedGem]);
	}
	
	public void NextGem()
	{
		SelectedGem++;
		if (SelectedGem >= GemsCollection.Gems.Count)
		{
			SelectedGem = 0;
		}
		GemAnimator.SetAnimation(GemsCollection.Gems[SelectedGem]);
	}
	
	public void PreviousGem()
	{
		SelectedGem--;
		if (SelectedGem < 0)
		{
			SelectedGem = GemsCollection.Gems.Count - 1;
		}
		GemAnimator.SetAnimation(GemsCollection.Gems[SelectedGem]);
	}
	
	public void SaveSettings()
	{
		selectedNickname = NicknameInput.text;
		PlayerPrefs.SetInt("SelectedGem", SelectedGem);
		PlayerPrefs.SetString("SelectedNickname", selectedNickname);
	}
}
