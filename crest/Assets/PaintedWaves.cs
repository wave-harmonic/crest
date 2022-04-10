using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

public class PaintedWaves : MonoBehaviour
{
    [SerializeField] float size = 128f;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Scale(size * Vector3.one);
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0f, 1f));

        //if (_type == OceanDepthCacheType.Realtime)
        //{
        //    Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        //    Gizmos.DrawCube(Vector3.up * _cameraMaxTerrainHeight / transform.lossyScale.y, new Vector3(1f, 0f, 1f));
        //}
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
class PaintedWavesInspector : Editor
{
    private void OnSceneGUI()
    {
        if (ToolManager.activeToolType == typeof(WavePaintingEditorTool))
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
    }

    //bool _painting = false;

    //public override void OnInspectorGUI()
    //{
    //    base.OnInspectorGUI();

    //    var editTxt = _painting ? "Stop Painting" : "Start Painting";
    //    if (GUILayout.Button(editTxt))
    //    {
    //        _painting = !_painting;
    //    }
    //}
}
#endif
