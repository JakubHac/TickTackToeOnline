using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class Initializer : MonoBehaviour
{
	private static Initializer Instance;

	private enum ForceMode
	{
		None,
		Server,
		Client
	}

	[SerializeField] private ForceMode ForcedMode;
	[SerializeField] private float ClientDelay = 1f;
	
	public static bool isServer
	{
		get
		{
			return Instance.ForcedMode switch
			{
				_ when !Application.isEditor => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null,
				ForceMode.None => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null,
				ForceMode.Server => true,
				ForceMode.Client => false
			};
		}
	}
	public static bool isClient => !isServer;
	
	private void Start()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Debug.LogError("Multiple Initializers found!", this.gameObject);
			return;
		}
		
		string sceneToLoad = isServer ? "ServerScene" : "ClientScene";
		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
		if (isClient)
		{
			asyncOperation.allowSceneActivation = false;
			StartCoroutine(DelayClientStart(asyncOperation));
		}
	}

	private IEnumerator DelayClientStart(AsyncOperation asyncOperation)
	{
		for (int i = 0; i < 5; i++)
		{
			yield return null;
		}
		yield return new WaitForSecondsRealtime(ClientDelay);
		asyncOperation.allowSceneActivation = true;
	}
}
