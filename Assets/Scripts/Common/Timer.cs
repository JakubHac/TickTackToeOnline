using UnityEngine;

public class Timer : MonoBehaviour
{
    private void Start()
    {
        TimeSinceStartup = Time.realtimeSinceStartupAsDouble;
        DontDestroyOnLoad(this.gameObject);
    }

    public static double TimeSinceStartup;

    private void Update()
    {
        TimeSinceStartup = Time.realtimeSinceStartupAsDouble;
    }
}
