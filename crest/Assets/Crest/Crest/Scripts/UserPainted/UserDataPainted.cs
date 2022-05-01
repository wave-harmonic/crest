using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    // Interface that clients use to call into this component
    public interface IUserAuthoredInput
    {
        void PrepareMaterial(Material mat);
        void UpdateMaterial(Material mat);
    }

    // Interface that this component uses to find clients and determine their data requirements
    public interface IPaintedDataClient
    {
        UnityEngine.Experimental.Rendering.GraphicsFormat GraphicsFormat { get; }
        ComputeShader PaintShader { get; }
    }

    // TODO - maybe rename? UserDataPainted and UserDataSpline would have been a systematic naming. However not sure
    // if this is user friendly, and not sure if it makes sense if we dont rename the Spline.
    // TODO - this component has no Enabled checkbox because enabling/disabling would need handling in terms of updating the
    // material keywords, and I'm unsure how best for this communication to happen
    // Made separate component as it matches the spline input (somewhat) and also it gives all the editor functionality below which may be painful
    // to apply to all our component types? Assuming it stays this way, then we need a way for it to be query the required data type and any
    // behaviour modifications.
    [ExecuteAlways]
    public class UserDataPainted : MonoBehaviour, IUserAuthoredInput
    {
        [Header("Settings")]
        public float _size = 256f;
        public int _resolution = 256;

        [Header("Painting")]

        [Range(0f, 1f)]
        public float _brushStrength = 0.75f;

        [Range(0.25f, 100f, 5f)]
        public float _brushRadius = 5f;

        [Range(1f, 100f, 5f)]
        public float _brushHardness = 1f;

        [System.NonSerialized]
        public CPUTexture2D<float> _tex = new CPUTexture2D<float>();

        private void OnEnable()
        {
            _tex.Resolution = new Vector2Int(_resolution, _resolution);
            _tex.WorldSize = new Vector2(_size, _size);
            _tex.CenterPosition = new Vector2(transform.position.x, transform.position.z);
            _tex.InitialiseDataIfNeeded();
        }

        public void PrepareMaterial(Material mat)
        {
            mat.EnableKeyword("_PAINTED_ON");

            mat.SetTexture("_PaintedWavesData", _tex.GPUTexture(GraphicsFormat.R16_SFloat, CPUTexture2DHelpers.ColorConstructFnOneChannel));
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
            mat.SetTexture("_PaintedWavesData", _tex.GPUTexture(GraphicsFormat.R16_SFloat, CPUTexture2DHelpers.ColorConstructFnOneChannel));
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
    // This typeof means it gets activated for the above component
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
    }

    // Additively blend mouse motion vector onto RG16F. Vector size < 1 used as wave weight.
    // Weight could also ramp up when motion vector confidence is low. Motion vector could lerp towards
    // current delta each frame.
    [CustomEditor(typeof(UserDataPainted))]
    class PaintedWavesEditor : Editor
    {
        Transform _cursor;

        private void OnEnable()
        {
            _cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            _cursor.gameObject.hideFlags = HideFlags.HideAndDontSave;
            _cursor.GetComponent<Renderer>().material = new Material(Shader.Find("Crest/PaintCursor"));
        }

        void ClearData()
        {
            (target as UserDataPainted)._tex.Clear(0f);
        }

        private void OnDisable()
        {
            DestroyImmediate(_cursor.gameObject);
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
                    OnMouseMove(false);
                    break;
                case EventType.MouseDown:
                    // Boost strength of mouse down, feels much better when clicking
                    OnMouseMove(Event.current.button == 0, 3f);
                    break;
                case EventType.MouseDrag:
                    OnMouseMove(Event.current.button == 0);
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

        void OnMouseMove(bool dragging, float weightMultiplier = 1f)
        {
            if (!OceanRenderer.Instance) return;

            var waves = target as UserDataPainted;
            var wavesPos = waves.transform.position;

            if (!WorldPosFromMouse(Event.current.mousePosition, out Vector3 pt))
            {
                return;
            }

            _cursor.position = pt;
            _cursor.localScale = new Vector3(2f, 0.25f, 2f) * waves._brushRadius;

            // The tex could instead be given a transform to take the position from. Maybe that's better.
            waves._tex.CenterPosition = new Vector2(wavesPos.x, wavesPos.z);

            float result = 0f;
            if (waves._tex.Sample(_cursor.position, CPUTexture2DHelpers.BilinearInterpolateFloat, ref result))
            {
                Debug.DrawLine(_cursor.position, _cursor.position + Vector3.up * result);
            }

            if (dragging && WorldPosFromMouse(Event.current.mousePosition - Event.current.delta, out Vector3 ptLast))
            {
                Vector2 dir;
                dir.x = pt.x - ptLast.x;
                dir.y = pt.z - ptLast.z;
                dir.Normalize();

                Vector2 uv;
                uv.x = (pt.x - waves.transform.position.x) / waves._size + 0.5f;
                uv.y = (pt.z - waves.transform.position.z) / waves._size + 0.5f;

                // TODO
                var remove = Event.current.shift ? 0.06f : 0f;

                waves._tex.PaintSmoothstep(pt, waves._brushRadius, weightMultiplier * 0.1f, waves._brushStrength, CPUTexture2DHelpers.PaintFnAdditiveBlendFloat);
            }
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
