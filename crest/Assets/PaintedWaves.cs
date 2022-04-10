using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Crest;
using UnityEngine.Rendering;

[ExecuteAlways]
public class PaintedWaves : MonoBehaviour
{
    public float _size = 256f;
    public int _resolution = 256;

    public RenderTexture _data;

    private void Update()
    {
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.Translate(transform.position) * Matrix4x4.Scale(_size * Vector3.one);
        Gizmos.color = WavePaintingEditorTool.CurrentlyPainting ? new Color(1f, 0f, 0f, 0.5f) : new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0f, 1f));
    }
#endif
}

#if UNITY_EDITOR
[EditorTool("Crest Wave Painting", typeof(PaintedWaves))]
class WavePaintingEditorTool : EditorTool
{
    PaintedWaves _waves;

    public override GUIContent toolbarIcon => _toolbarIcon ??
        (_toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PaintedWaves.png"), "Crest Wave Painting"));

    public static bool CurrentlyPainting => ToolManager.activeToolType == typeof(WavePaintingEditorTool);

    GUIContent _toolbarIcon;

    private void OnEnable()
    {
        ToolManager.activeToolChanged += ActiveToolDidChange;
    }
    private void OnDisable()
    {
        ToolManager.activeToolChanged -= ActiveToolDidChange;
    }

    void ActiveToolDidChange()
    {
        if (!ToolManager.IsActiveTool(this))
            return;

        _waves = target as PaintedWaves;
    }

    //public override void OnToolGUI(EditorWindow window)
    //{
    //    EditorGUI.BeginChangeCheck();

    //    var pos = Tools.handlePosition;

    //    using (new Handles.DrawingScope(Color.green))
    //    {
    //        pos = Handles.Slider(pos, Vector3.right);
    //    }

    //    if (EditorGUI.EndChangeCheck())
    //    {
    //        var delta = pos - Tools.handlePosition;

    //        Undo.RecordObjects(Selection.transforms, "Crest Wave Painting");

    //        foreach (var transform in Selection.transforms)
    //        {
    //            transform.position += delta;
    //        }
    //    }
    //}

    public override void OnToolGUI(EditorWindow window)
    {
        //var evt = Event.current;

        //if (evt.type == EventType.Repaint)
        //{
        //    var zTest = Handles.zTest;
        //    Handles.zTest = CompareFunction.LessEqual;

        //    foreach (var entry in _vertices)
        //    {
        //        foreach (var vertex in entry._positions)
        //        {
        //            var world = entry._transform.TransformPoint(vertex);
        //            Handles.DotHandleCap(0, world, Quaternion.identity, HandleUtility.GetHandleSize(world) * .05f, evt.type);
        //        }
        //    }

        //    Handles.zTest = zTest;
        //}
    }
}

// Additively blend mouse motion vector onto RG16F. Vector size < 1 used as wave weight.
// Weight could also ramp up when motion vector confidence is low. Motion vector could lerp towards
// current delta each frame.
[CustomEditor(typeof(PaintedWaves))]
class PaintedWavesEditor : Editor
{
    Transform _preview;

    Transform _cursor;
    ComputeShader _paintShader;
    int _kernel = 0;

    Material _previewMat;

    CommandBuffer _cmdBuf;
    CommandBuffer CommandBuffer
    {
        get
        {
            if (_cmdBuf == null)
            {
                _cmdBuf = new UnityEngine.Rendering.CommandBuffer();
            }
            _cmdBuf.name = "Paint Waves";
            return _cmdBuf;
        }
    }

    private void OnEnable()
    {
        _cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        _cursor.gameObject.hideFlags = HideFlags.HideAndDontSave;

        _preview = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        _preview.localScale = Vector3.one * 50f;
        // Can't rotate??
        _preview.eulerAngles = 0f * Vector3.right;
        _preview.gameObject.hideFlags = HideFlags.HideAndDontSave;
        _previewMat = new Material(Shader.Find("Unlit/Texture"));
        _preview.GetComponent<Renderer>().material = _previewMat;

        if (_paintShader == null)
        {
            _paintShader = ComputeShaderHelpers.LoadShader("PaintWaves");
        }


        var waves = target as PaintedWaves;
        //if (waves._data == null || waves._data.width != waves._resolution || waves._data.height != waves._resolution)
        {
            //waves._data = new RenderTexture(waves._resolution, waves._resolution, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            waves._data = new RenderTexture(waves._resolution, waves._resolution, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_SFloat);
            waves._data.enableRandomWrite = true;
            waves._data.Create();

            CommandBuffer.Clear();
            CommandBuffer.SetRenderTarget(waves._data);
            CommandBuffer.ClearRenderTarget(true, true, Color.black);
            Graphics.ExecuteCommandBuffer(CommandBuffer);
        }
        _previewMat.mainTexture = waves._data;

    }

    private void OnDisable()
    {
        DestroyImmediate(_cursor.gameObject);
        DestroyImmediate(_preview.gameObject);
    }

    private void OnSceneGUI()
    {
        if (ToolManager.activeToolType != typeof(WavePaintingEditorTool))
        {
            return;
        }

        switch (Event.current.type)
        {
            case EventType.MouseMove:
                OnMouseDrag(false);
                break;
            case EventType.MouseDrag:
                OnMouseDrag(true);
                break;
            case EventType.Layout:
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                break;
        }
    }

    void OnMouseDrag(bool dragging)
    {
        var ocean = Crest.OceanRenderer.Instance;
        if (!ocean) return;

        var e = Event.current;
        var r = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        var heightOffset = r.origin.y - ocean.transform.position.y;
        var diry = r.direction.y;
        if (heightOffset * diry >= 0f)
        {
            // Ray going away from ocean plane
            return;
        }

        var dist = -heightOffset / diry;
        var pt = r.GetPoint(dist);
        _cursor.position = pt;

        if (dragging)
        {
            var waves = target as PaintedWaves;
            Vector2 uv;
            uv.x = (pt.x - waves.transform.position.x) / waves._size + 0.5f;
            uv.y = (pt.z - waves.transform.position.z) / waves._size + 0.5f;
            Paint(waves, uv);
        }
    }

    void Paint(PaintedWaves waves, Vector2 uv)
    {
        CommandBuffer.Clear();

        //if (waves._data == null || waves._data.width != waves._resolution || waves._data.height != waves._resolution)
        //{
        //    //waves._data = new RenderTexture(waves._resolution, waves._resolution, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        //    waves._data = new RenderTexture(waves._resolution, waves._resolution, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_SFloat);
        //    waves._data.enableRandomWrite = true;
        //    waves._data.Create();

        //    CommandBuffer.SetRenderTarget(waves._data);
        //    CommandBuffer.ClearRenderTarget(true, true, Color.white);
        //}

        CommandBuffer.SetComputeFloatParam(_paintShader, "_RadiusUV", 0.05f);
        CommandBuffer.SetComputeVectorParam(_paintShader, "_PaintUV", uv);
        CommandBuffer.SetComputeTextureParam(_paintShader, _kernel, "_Result", waves._data);
        CommandBuffer.DispatchCompute(_paintShader, _kernel, (waves._data.width + 7) / 8, (waves._data.height + 7) / 8, 1);
        Graphics.ExecuteCommandBuffer(CommandBuffer);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (WavePaintingEditorTool.CurrentlyPainting)
        {
            if (GUILayout.Button("Stop Painting"))
            {
                ToolManager.RestorePreviousPersistentTool();
            }
        }
        else
        {
            if (GUILayout.Button("Start Painting"))
            {
                ToolManager.SetActiveTool<WavePaintingEditorTool>();
            }
        }
    }
}
#endif
