using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine.Rendering;

namespace Crest
{
    public interface IUserAuthoredInput
    {
        void PrepareMaterial(Material mat);
        void UpdateMaterial(Material mat);
    }

    // TODO - maybe rename? UserDataPainted and UserDataSpline would have been a systematic naming. However not sure
    // if this is user friendly, and not sure if it makes sense if we dont rename the Spline.
    [ExecuteAlways]
    public class UserDataPainted : MonoBehaviour, IUserAuthoredInput
    {
        public float _size = 256f;
        public int _resolution = 256;

        [Range(0.25f, 100f, 5f)]
        public float _brushRadius = 5f;

        [Range(1f, 100f, 5f)]
        public float _brushHardness = 1f;

        // TODO - made nonserialised as behaviour is pretty buggy when on. Reloading a scene
        // seems to kill the data. Perhaps needs to be Texture2D?
        [System.NonSerialized]
        public RenderTexture _data;

        private void Update()
        {
        }

        public void PrepareMaterial(Material mat)
        {
            // TODO - enable keywords to make material use the spline geo. Right now
            // there is a completely separate shader for the spline/ge case which does not scale
            // and requires specific treatment in the code.
            mat.SetTexture("_PaintedWavesData", _data);
            mat.SetFloat("_PaintedWavesSize", _size);

            Vector2 pos;
            pos.x = transform.position.x;
            pos.y = transform.position.z;
            mat.SetVector("_PaintedWavesPosition", pos);
        }

        public void UpdateMaterial(Material mat)
        {
#if UNITY_EDITOR
            // Any per-frame update. In editor keep it all fresh.
            mat.SetTexture("_PaintedWavesData", _data);
            mat.SetFloat("_PaintedWavesSize", _size);

            Vector2 pos;
            pos.x = transform.position.x;
            pos.y = transform.position.z;
            mat.SetVector("_PaintedWavesPosition", pos);
#endif
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
    [EditorTool("Crest Wave Painting", typeof(UserDataPainted))]
    class WavePaintingEditorTool : EditorTool
    {
        UserDataPainted _waves;

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

            _waves = target as UserDataPainted;
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
    [CustomEditor(typeof(UserDataPainted))]
    class PaintedWavesEditor : Editor
    {
        //Transform _preview;

        Transform _cursor;
        ComputeShader _paintShader;
        int _kernel = 0;

        //Material _previewMat;

        Vector3 _motionVector;

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
            _cursor.GetComponent<Renderer>().material = new Material(Shader.Find("Crest/PaintCursor"));

            // This created a plane to try to draw a preview of the texture in the world. What is most likely needed
            // is debug code in the ocean material to visualise it on the water itself as the plane was ugly.
            //_preview = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            //_preview.localScale = Vector3.one * 50f;
            //// Can't rotate??
            //_preview.eulerAngles = 90f * Vector3.right;
            //_preview.gameObject.hideFlags = HideFlags.HideAndDontSave;
            //_preview.position = (target as PaintedWaves).transform.position + Vector3.up * 10f;
            //_previewMat = new Material(Shader.Find("Unlit/Texture"));
            //_preview.GetComponent<Renderer>().material = _previewMat;

            if (_paintShader == null)
            {
                _paintShader = ComputeShaderHelpers.LoadShader("PaintWaves");
            }


            var waves = target as UserDataPainted;
            if (waves._data == null || waves._data.width != waves._resolution || waves._data.height != waves._resolution)
            {
                // TODO 
                waves._data = new RenderTexture(waves._resolution, waves._resolution, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_SFloat);
                waves._data.enableRandomWrite = true;
                waves._data.Create();

                //ClearData();
            }
            //_previewMat.mainTexture = waves._data;

        }

        void ClearData()
        {
            CommandBuffer.Clear();
            CommandBuffer.SetRenderTarget((target as UserDataPainted)._data);
            CommandBuffer.ClearRenderTarget(true, true, Color.black);
            Graphics.ExecuteCommandBuffer(CommandBuffer);
        }

        private void OnDisable()
        {
            DestroyImmediate(_cursor.gameObject);
            //DestroyImmediate(_preview.gameObject);
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
                    OnMouseDrag(Event.current.button == 0);
                    break;
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                    break;
            }
        }

        bool WorldPosFromMouse(Vector2 mousePos, out Vector3 pos)
        {
            var r = HandleUtility.GUIPointToWorldRay(mousePos);

            var heightOffset = r.origin.y - OceanRenderer.Instance.transform.position.y;
            var diry = r.direction.y;
            if (heightOffset * diry >= 0f)
            {
                // Ray going away from ocean plane
                pos = Vector3.zero;
                return false;
            }

            var dist = -heightOffset / diry;
            pos = r.GetPoint(dist);
            return true;
        }

        void OnMouseDrag(bool dragging)
        {
            if (!OceanRenderer.Instance) return;

            var waves = target as UserDataPainted;

            if (!WorldPosFromMouse(Event.current.mousePosition, out Vector3 pt))
            {
                return;
            }

            _cursor.position = pt;
            _cursor.localScale = 2f * Vector3.one * waves._brushRadius;

            if (dragging && WorldPosFromMouse(Event.current.mousePosition - Event.current.delta, out Vector3 ptLast))
            {
                Vector2 dir;
                dir.x = pt.x - ptLast.x;
                dir.y = pt.z - ptLast.z;

                Vector2 uv;
                uv.x = (pt.x - waves.transform.position.x) / waves._size + 0.5f;
                uv.y = (pt.z - waves.transform.position.z) / waves._size + 0.5f;
                Paint(waves, uv, dir);
            }
        }

        void Paint(UserDataPainted waves, Vector2 uv, Vector2 dir)
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

            CommandBuffer.SetComputeFloatParam(_paintShader, "_RadiusUV", waves._brushRadius / waves._size);
            CommandBuffer.SetComputeFloatParam(_paintShader, "_BrushHardness", waves._brushHardness);
            CommandBuffer.SetComputeVectorParam(_paintShader, "_PaintUV", uv);
            CommandBuffer.SetComputeVectorParam(_paintShader, "_PaintDirection", dir);
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

            if (GUILayout.Button("Clear"))
            {
                ClearData();
            }
        }
    }
#endif
}
