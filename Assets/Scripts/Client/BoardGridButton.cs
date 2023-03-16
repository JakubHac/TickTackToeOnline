using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGridButton : MonoBehaviour
{
	[SerializeField] private RoomGameplayController gameplay;
	
	public void Clicked()
	{
		gameplay.ClickedGridButton(transform.GetSiblingIndex());
	}
	
}
