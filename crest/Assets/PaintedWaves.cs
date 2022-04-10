using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

[ExecuteAlways]
public class PaintedWaves : MonoBehaviour
{
    [SerializeField] float _size = 256f;
    [SerializeField] int _resolution = 256;

    RenderTexture _data;

    private void Update()
    {
        if (_data == null || _data.width != _resolution || _data.height != _resolution)
        {
            _data = new RenderTexture(_resolution, _resolution, 0);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Scale(_size * Vector3.one);
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

[CustomEditor(typeof(PaintedWaves))]
class PaintedWavesEditor : Editor
{
    Transform _cursor;

    private void OnEnable()
    {
        //    SceneView.duringSceneGui += SceneView_duringSceneGui;
        _cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        _cursor.gameObject.hideFlags = HideFlags.HideAndDontSave;
    }

    private void OnDisable()
    {
        //    SceneView.duringSceneGui -= SceneView_duringSceneGui;
        GameObject.DestroyImmediate(_cursor.gameObject);
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
