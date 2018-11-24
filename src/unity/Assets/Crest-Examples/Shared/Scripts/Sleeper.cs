using UnityEngine;

public class Sleeper : MonoBehaviour
{
    public int _sleepMs = 0;
    public bool _jitter = false;

	void Update()
    {
        int sleep = _jitter ? (int)(Random.value * _sleepMs) : _sleepMs;
        System.Threading.Thread.Sleep(sleep);
	}
}
