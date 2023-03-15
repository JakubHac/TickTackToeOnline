using UnityEditor;
using UnityEngine;

public class AppQuit : MonoBehaviour
{
	public void Quit()
	{
#if UNITY_EDITOR
		EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}