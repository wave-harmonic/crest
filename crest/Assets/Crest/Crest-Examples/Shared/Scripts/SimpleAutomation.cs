// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR

public class SimpleAutomation : MonoBehaviour
{
    static bool _reloadPending = true;

    public int _pauseOnFrame = -1;
    public float _pauseAtTime = -1f;

    void Update()
    {
        if (_reloadPending && Time.time > 2f)
        {
            SceneManager.LoadScene(SceneManager.GetSceneAt(0).buildIndex);
            _reloadPending = false;
        }

        if (_pauseOnFrame != -1 && Time.frameCount >= _pauseOnFrame)
        {
            UnityEditor.EditorApplication.isPaused = true;
        }

        if (_pauseAtTime != -1f && Time.time >= _pauseAtTime)
        {
            UnityEditor.EditorApplication.isPaused = true;
        }
    }
}

#endif
