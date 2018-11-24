using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

public class TestReadback : MonoBehaviour {

    public RenderTextureFormat _fmt;
    public TextureFormat _fmt2;

    public RenderTexture[] _rt;
    public Texture2D[] _tex;
    GameObject _marker;

    int _numRTs = 6;

	void Start () {
        int w, h;
        w = h = 512;

        _rt = new RenderTexture[_numRTs];
        _tex = new Texture2D[_numRTs];
        for (var i = 0; i < _numRTs; i++)
        {
            _tex[i] = new Texture2D(w, h, _fmt2, false)
            {
                name = "ReadPixels_" + i
            };

            _rt[i] = new RenderTexture(w, h, 0, _fmt)
            {
                name = "ReadbackTestTarget_" + i
            };
        }

        _marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(_marker.GetComponent<Collider>());
        _marker.transform.localScale = 10f * Vector3.one;
    }

    float timeTaken = 0f;
    public bool _readPixels = true;
    public bool _samplePixel = true;
    public int _readFramesBehind = 2;

    public Queue<AsyncGPUReadbackRequest> _requests = new Queue<AsyncGPUReadbackRequest>();
    public int _successCount = 0;
    public int _errorCount = 0;

    public bool _useAsync = true;
    public bool _throttleFramerate = true;

    Color _lastSample;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_useAsync)
        {
            if (_requests.Count < 8)
                _requests.Enqueue(AsyncGPUReadback.Request(source));
            else
                Debug.Log("Too many requests.");
        }

        Graphics.Blit(source, destination);
    }

    private void Update()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        int writeIndex = Time.frameCount % _numRTs;
        GetComponent<Camera>().targetTexture = _rt[writeIndex];

        if(_useAsync)
        {
            //_requests.Enqueue(AsyncGPUReadback.Request(_rt[readIndex], 0));

            // clear out any errors
            int maxRemoves = 8;
            for (int i = 0; i < maxRemoves && _requests.Count > 0; i++)
            {
                var request = _requests.Peek();
                if (request.hasError)
                {
                    ++_errorCount;
                    _requests.Dequeue();
                }
                else
                {
                    break;
                }
            }

            Unity.Collections.NativeArray<ushort>? data = null;

            if (_requests.Count > 0)
            {
                var request = _requests.Peek();

                if (request.done)
                {
                    ++_successCount;
                    data = request.GetData<ushort>();
                    _requests.Dequeue();
                }
            }

            if (data.HasValue)
            {
                var x = 250f; var z = 250f;
                //var pix = _tex[readIndex].GetPixelBilinear(x / 500f, z / 500f);
                int centerIdx = _rt[0].width * _rt[0].height / 2 + _rt[0].width / 2;
                //_lastSample = data.Value[centerIdx];
                //_lastSample = data.Value[Random.Range(0, data.Value.Length)];
                _lastSample[0] = Mathf.HalfToFloat(data.Value[centerIdx * 4 + 0]);
                _lastSample[1] = Mathf.HalfToFloat(data.Value[centerIdx * 4 + 1]);
                _lastSample[2] = Mathf.HalfToFloat(data.Value[centerIdx * 4 + 2]);
                _marker.transform.position = new Vector3(x + _lastSample.r, _lastSample.g, z + _lastSample.b);
            }
        }
        else
        {
            int readIndex = (Time.frameCount - _readFramesBehind) % _numRTs;

            if (_readPixels)
            {
                var oldRT = RenderTexture.active;
                RenderTexture.active = _rt[readIndex];
                _tex[readIndex].ReadPixels(new Rect(0, 0, _rt[readIndex].width, _rt[readIndex].height), 0, 0, false);
                RenderTexture.active = oldRT;
            }

            if (_samplePixel)
            {
                var x = 250f; var z = 250f;
                _lastSample = _tex[readIndex].GetPixelBilinear(x / 500f, z / 500f);
                _marker.transform.position = new Vector3(x + _lastSample.r, _lastSample.g, z + _lastSample.b);
            }
        }

        timeTaken = sw.ElapsedMilliseconds;

        if(_throttleFramerate)
        {
            while (sw.ElapsedMilliseconds < 30) { }
        }
    }

    private void OnGUI()
    {
        float y = 0f;
        float w = 300f, h = 25f;

        _useAsync = GUI.Toggle(new Rect(0, y, w, h), _useAsync, "Use Async"); y += h;
        _throttleFramerate = GUI.Toggle(new Rect(0, y, w, h), _throttleFramerate, "Throttle framerate"); y += h;
        GUI.Label(new Rect(0, y, w, h), "ReadTime: " + timeTaken + "ms"); y += h;

        if (!_useAsync)
        {
            _readPixels = GUI.Toggle(new Rect(0, y, w, h), _readPixels, "ReadPixels"); y += h;
            _samplePixel = GUI.Toggle(new Rect(0, y, w, h), _samplePixel, "SamplePixel"); y += h;
        }

        GUI.Label(new Rect(0, y, w, h), "Frames behind: " + _readFramesBehind); y += h;
        _readFramesBehind = Mathf.FloorToInt(GUI.HorizontalSlider(new Rect(0, y, w, h), _readFramesBehind, -(_numRTs - 1), (_numRTs - 1))); y += h;

        GUI.Label(new Rect(0, y, w, h), "Last: " + _lastSample.ToString()); y += h;

        if (_useAsync)
        {
            GUI.Label(new Rect(0, y, w, h), "Queue: " + _requests.Count); y += h;
            GUI.Label(new Rect(0, y, w, h), "Successes: " + _successCount); y += h;
            GUI.Label(new Rect(0, y, w, h), "Errors: " + _errorCount); y += h;
            if (_requests.Count > 0 && GUI.Button(new Rect(0, y, w, h), "Drop one"))
            {
                _requests.Dequeue();
            }
            y += h;
            if (_requests.Count > 0 && GUI.Button(new Rect(0, y, w, h), "Drop all"))
            {
                _requests.Clear();
            }
            y += h;
        }
    }
}
