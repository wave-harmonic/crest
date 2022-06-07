// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Crest.Spline;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    using OceanInput = CrestSortedList<int, ILodDataInput>;

    /// <summary>
    /// Comparer that always returns less or greater, never equal, to get work around unique key constraint
    /// </summary>
    public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
    {
        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            // If non-zero, use result, otherwise return greater (never equal)
            return result != 0 ? result : 1;
        }
    }

    public interface ILodDataInput
    {
        /// <summary>
        /// Draw the input (the render target will be bound)
        /// </summary>
        void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx);

        /// <summary>
        /// The wavelength of the input - used to choose which level of detail to apply the input to.
        /// This is primarily used for displacement/surface shape inputs which support rendering to
        /// specific LODs (which are then combined later). Specify 0 for no preference / render to all LODs.
        /// </summary>
        float Wavelength { get; }

        /// <summary>
        /// Whether to apply this input.
        /// </summary>
        bool Enabled { get; }
    }

    /// <summary>
    /// Base class for scripts that register input to the various LOD data types.
    /// </summary>
    [ExecuteAlways]
    public abstract partial class RegisterLodDataInputBase : MonoBehaviour, ILodDataInput
    {
        public enum InputMode
        {
            Autodetect = 0,
            Painted,
            Spline,
            CustomGeometryAndShader,
            Primitive
        }

        [Header("Mode")]
        [Filtered]
        public InputMode _inputModeUserFacing = InputMode.Autodetect;
        [HideInInspector]
        public InputMode _inputMode = InputMode.Autodetect;

        public virtual InputMode DefaultMode => InputMode.Painted;

        public bool ShowPaintingUI => _inputMode == InputMode.Painted;

#if UNITY_EDITOR
        [Header("Custom Geometry And Shader Mode Settings")]
        [SerializeField, Tooltip("Check that the shader applied to this object matches the input type (so e.g. an Animated Waves input object has an Animated Waves input shader.")]
        [Predicated("_inputMode", inverted: true, InputMode.CustomGeometryAndShader), DecoratedField]
        bool _checkShaderName = true;

        [SerializeField, Tooltip("Check that the shader applied to this object has only a single pass as only the first pass is executed for most inputs.")]
        [Predicated("_inputMode", inverted: true, InputMode.CustomGeometryAndShader), DecoratedField]
        bool _checkShaderPasses = true;
#endif

        public const string MENU_PREFIX = Internal.Constants.MENU_SCRIPTS + "LOD Inputs/Crest Register ";

        public abstract float Wavelength { get; }

        public abstract bool Enabled { get; }

        public static int sp_Weight = Shader.PropertyToID("_Weight");
        public static int sp_DisplacementAtInputPosition = Shader.PropertyToID("_DisplacementAtInputPosition");

        // By default do not follow horizontal motion of waves. This means that the ocean input will appear on the surface at its XZ location, instead
        // of moving horizontally with the waves.
        protected virtual bool FollowHorizontalMotion => false;

        protected abstract string ShaderPrefix { get; }

        protected abstract Color GizmoColor { get; }

        static DuplicateKeyComparer<int> s_comparer = new DuplicateKeyComparer<int>();
        static Dictionary<Type, OceanInput> s_registrar = new Dictionary<Type, OceanInput>();

        public static OceanInput GetRegistrar(Type lodDataMgrType)
        {
            if (!s_registrar.TryGetValue(lodDataMgrType, out var registered))
            {
                registered = new OceanInput(s_comparer);
                s_registrar.Add(lodDataMgrType, registered);
            }
            return registered;
        }

        internal Renderer _renderer;
        protected Material _paintInputMaterial;
        // We pass this to GetSharedMaterials to avoid allocations.
        protected List<Material> _sharedMaterials = new List<Material>();
        SampleHeightHelper _sampleHelper = new SampleHeightHelper();

        protected virtual bool SupportsMultiPassShaders => false;

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        public virtual void AutoDetectMode(out InputMode mode)
        {
            if (TryGetComponent<Renderer>(out _))
            {
                mode = InputMode.CustomGeometryAndShader;
            }
            else
            {
                mode = DefaultMode;
            }
        }

        protected virtual void Awake()
        {
            if (_inputModeUserFacing != InputMode.Autodetect)
            {
                _inputMode = _inputModeUserFacing;
            }
            else
            {
                AutoDetectMode(out _inputMode);
            }
        }

        // Called when component attached in edit mode, or when Reset clicked by user.
        protected void Reset()
        {
            AutoDetectMode(out _inputModeUserFacing);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (_inputMode == InputMode.CustomGeometryAndShader)
            {
                if (TryGetComponent<MeshFilter>(out var mf))
                {
                    Gizmos.color = GizmoColor;
                    Gizmos.DrawWireMesh(mf.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
                }
            }

            if (_inputMode == InputMode.Painted)
            {
                if (this is IPaintable paintable)
                {
                    PaintableEditor.DrawPaintAreaGizmo(paintable, GizmoColor);
                }
            }
        }

        void InitRendererAndMaterial(bool verifyShader)
        {
            if (_inputMode == InputMode.CustomGeometryAndShader)
            {
                _renderer = GetComponent<Renderer>();

                if (_renderer != null)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying && verifyShader)
                    {
                        ValidatedHelper.ValidateRenderer<Renderer>(gameObject, ValidatedHelper.DebugLog, false, SupportsMultiPassShaders, _checkShaderName ? ShaderPrefix : String.Empty);
                    }
#endif
                }
            }
            else if (_inputMode == InputMode.Painted)
            {
                var paintable = this as IPaintable;
                var paintedInputShader = paintable?.PaintedInputShader;
                if (paintedInputShader)
                {
                    _paintInputMaterial = new Material(paintedInputShader);
                    PreparePaintInputMaterial(_paintInputMaterial);
                }
            }
        }

        protected virtual void PreparePaintInputMaterial(Material mat)
        {
        }

        protected virtual void UpdatePaintInputMaterial(Material mat)
        {
        }

        protected virtual void Start()
        {
            InitRendererAndMaterial(true);
        }

        protected virtual void Update()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                InitRendererAndMaterial(true);
            }

#endif

            if (_paintInputMaterial != null)
            {
                UpdatePaintInputMaterial(_paintInputMaterial);
            }
        }

        public virtual void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            if (weight == 0f)
            {
                return;
            }

            buf.SetGlobalFloat(sp_Weight, weight);
            buf.SetGlobalFloat(LodDataMgr.sp_LD_SliceIndex, lodIdx);

            if (!FollowHorizontalMotion)
            {
                // This can be called multiple times per frame - one for each LOD potentially
                _sampleHelper.Init(transform.position, 0f, true, this);
                _sampleHelper.Sample(out Vector3 displacement, out _, out _);
                buf.SetGlobalVector(sp_DisplacementAtInputPosition, displacement);
            }
            else
            {
                buf.SetGlobalVector(sp_DisplacementAtInputPosition, Vector3.zero);
            }

            if (_inputMode == InputMode.Painted)
            {
#if UNITY_EDITOR
                if (!(this is IPaintable))
                {
                    Debug.LogError($"Crest: {GetType().Name} component has invalid Input Mode setting, please set this to a supported option such as {DefaultMode}. Click this message to highlight the relevant GameObject.", this);
                }
#endif

                if (_paintInputMaterial)
                {
                    buf.DrawProcedural(Matrix4x4.identity, _paintInputMaterial, 0, MeshTopology.Triangles, 3);
                }
            }
            else if (_inputMode == InputMode.CustomGeometryAndShader)
            {
                if (_renderer)
                {
                    _renderer.GetSharedMaterials(_sharedMaterials);
                    for (var i = 0; i < _sharedMaterials.Count; i++)
                    {
                        // Empty material slots is a user error, but skip so we do not spam errors.
                        if (_sharedMaterials[i] == null)
                        {
                            continue;
                        }

                        // By default, shaderPass is -1 which is all passes. Shader Graph will produce multi-pass shaders
                        // for depth etc so we should only render one pass. Unlit SG will have the unlit pass first.
                        // Submesh count generally must equal number of materials.
                        buf.DrawRenderer(_renderer, _sharedMaterials[i], submeshIndex: i, shaderPass: 0);
                    }
                }
            }
            else if (_inputMode == InputMode.Autodetect)
            {
                // TODO update message
                //Debug.LogError($"Crest: {GetType().Name} has component does not have an Input Mode set, please set this to a supported option such as {DefaultMode}. Click this message to highlight the relevant GameObject.", this);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_registrar.Clear();
        }

        static Mesh s_Quad;
        /// <summary>
        /// Quad geometry
        /// </summary>
        public static Mesh QuadMesh
        {
            get
            {
                if (s_Quad) return s_Quad;

                return s_Quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            }
        }
    }

    /// <summary>
    /// Registers input to a particular LOD data.
    /// </summary>
    [ExecuteAlways]
    public abstract class RegisterLodDataInput<LodDataType> : RegisterLodDataInputBase
        where LodDataType : LodDataMgr
    {
        protected const string k_displacementCorrectionTooltip = "Whether this input data should displace horizontally with waves. If false, data will not move from side to side with the waves. Adds a small performance overhead when disabled.";

        [SerializeField, Predicated("_inputMode", inverted: true, InputMode.CustomGeometryAndShader), DecoratedField]
        bool _disableRenderer = true;

        int _registeredQueueValue = int.MinValue;

        protected virtual bool GetQueue(out int queue)
        {
            var rend = GetComponent<Renderer>();
            if (rend && rend.sharedMaterial != null)
            {
                queue = rend.sharedMaterial.renderQueue;
                return true;
            }
            queue = int.MinValue;
            return false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_disableRenderer)
            {
                var rend = GetComponent<Renderer>();
                if (rend)
                {
                    if (rend is TrailRenderer || rend is LineRenderer)
                    {
                        // If we disable using "enabled" then the line/trail positions will not be updated. This keeps
                        // the scripting side of the component running and just disables the rendering. Similar to
                        // disabling the Renderer module on the Particle System.
                        rend.forceRenderingOff = true;
                    }
                    else
                    {
                        rend.enabled = false;
                    }
                }
            }

            GetQueue(out var q);

            var registrar = GetRegistrar(typeof(LodDataType));
            registrar.Add(q, this);
            _registeredQueueValue = q;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            var registrar = GetRegistrar(typeof(LodDataType));
            if (registrar != null)
            {
                registrar.Remove(this);
            }
        }

        protected override void Update()
        {
            base.Update();

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                if (GetQueue(out var q))
                {
                    if (q != _registeredQueueValue)
                    {
                        var registrar = GetRegistrar(typeof(LodDataType));
                        registrar.Remove(this);
                        registrar.Add(q, this);
                        _registeredQueueValue = q;
                    }
                }
            }
#endif
        }
    }

    public abstract class RegisterLodDataInputWithSplineSupport<LodDataType>
        : RegisterLodDataInputWithSplineSupport<LodDataType, SplinePointDataNone>
        where LodDataType : LodDataMgr
    {
    }

    [ExecuteAlways]
    public abstract partial class RegisterLodDataInputWithSplineSupport<LodDataType, SplinePointCustomData>
        : RegisterLodDataInput<LodDataType>
        , ISplinePointCustomDataSetup
#if UNITY_EDITOR
        , IReceiveSplinePointOnDrawGizmosSelectedMessages
#endif
        where LodDataType : LodDataMgr
        where SplinePointCustomData : MonoBehaviour, ISplinePointCustomData
    {
        [Header("Spline Mode Settings")]
        [SerializeField, Predicated("_inputMode", inverted: true, InputMode.Spline), DecoratedField]
        bool _overrideSplineSettings = false;
        [SerializeField, Predicated("_overrideSplineSettings"), DecoratedField]
        float _radius = 20f;
        [SerializeField, Predicated("_overrideSplineSettings"), Delayed]
        int _subdivisions = 1;

        protected Material _splineMaterial;
        Spline.Spline _spline;
        Mesh _splineMesh;

        protected abstract string SplineShaderName { get; }
        protected abstract Vector2 DefaultCustomData { get; }

        protected float _splinePointHeightMin;
        protected float _splinePointHeightMax;

        protected override void Awake()
        {
            base.Awake();

            if (_inputMode == InputMode.Spline)
            {
                if (TryGetComponent(out _spline))
                {
                    var radius = _overrideSplineSettings ? _radius : _spline.Radius;
                    var subdivs = _overrideSplineSettings ? _subdivisions : _spline.Subdivisions;
                    ShapeGerstnerSplineHandling.GenerateMeshFromSpline<SplinePointCustomData>(_spline, transform, subdivs, radius, DefaultCustomData,
                        ref _splineMesh, out _splinePointHeightMin, out _splinePointHeightMax);

                    if (_splineMaterial == null)
                    {
                        CreateSplineMaterial();
                    }
                }
            }
        }

        public override void AutoDetectMode(out InputMode mode)
        {
            if (TryGetComponent<Spline.Spline>(out _))
            {
                mode = InputMode.Spline;
            }
            else
            {
                base.AutoDetectMode(out mode);
            }
        }

        protected virtual void CreateSplineMaterial()
        {
            _splineMaterial = new Material(Shader.Find(SplineShaderName));
        }

        public override void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            if (weight <= 0f) return;

            if (_inputMode == InputMode.Spline)
            {
                if (_splineMesh != null && _splineMaterial != null)
                {
                    buf.SetGlobalFloat(sp_Weight, weight);
                    buf.SetGlobalFloat(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                    buf.SetGlobalVector(sp_DisplacementAtInputPosition, Vector3.zero);
                    buf.DrawMesh(_splineMesh, transform.localToWorldMatrix, _splineMaterial);
                }
            }
            else
            {
                base.Draw(lodData, buf, weight, isTransition, lodIdx);
            }
        }

        public bool AttachDataToSplinePoint(GameObject splinePoint)
        {
            if (typeof(SplinePointCustomData) == typeof(SplinePointDataNone))
            {
                // No custom data required
                return false;
            }

            if (splinePoint.TryGetComponent(out SplinePointCustomData _))
            {
                // Already existing, nothing to do
                return false;
            }

            splinePoint.AddComponent<SplinePointCustomData>();
            return true;
        }

#if UNITY_EDITOR
        protected override void Update()
        {
            base.Update();

            // Check for spline and rebuild spline mesh each frame in edit mode
            if (!EditorApplication.isPlaying)
            {
                if (_inputMode == InputMode.Spline)
                {
                    UpdateSpline();
                }
            }
        }

        void UpdateSpline()
        {
            if (_spline == null)
            {
                TryGetComponent(out _spline);
            }

            if (_spline != null)
            {
                var radius = _overrideSplineSettings ? _radius : _spline.Radius;
                var subdivs = _overrideSplineSettings ? _subdivisions : _spline.Subdivisions;
                ShapeGerstnerSplineHandling.GenerateMeshFromSpline<SplinePointCustomData>(_spline, transform, subdivs, radius, DefaultCustomData,
                    ref _splineMesh, out _splinePointHeightMin, out _splinePointHeightMax);

                if (_splineMaterial == null)
                {
                    CreateSplineMaterial();
                }
            }
            else
            {
                _splineMesh = null;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (_inputMode == InputMode.Spline)
            {
                if (_splineMesh != null)
                {
                    Gizmos.color = GizmoColor;
                    Gizmos.DrawWireMesh(_splineMesh, transform.position, transform.rotation, transform.lossyScale);
                }
            }
        }

        public void OnSplinePointDrawGizmosSelected(SplinePoint point)
        {
            OnDrawGizmosSelected();
        }
#endif // UNITY_EDITOR
    }

#if UNITY_EDITOR
    public abstract partial class RegisterLodDataInputBase : IValidated
    {
        protected virtual string FeatureToggleLabel => null;
        protected virtual string FeatureToggleName => null;
        protected virtual bool FeatureEnabled(OceanRenderer ocean) => true;

        protected virtual string RequiredShaderKeyword => null;
        // NOTE: Temporary until shader keywords are the same across pipelines.
        protected virtual string RequiredShaderKeywordProperty => null;

        protected virtual string MaterialFeatureDisabledError => null;
        protected virtual string MaterialFeatureDisabledFix => null;

        public virtual bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            //if (_inputMode == InputMode.Unset)
            //{
            //    showMessage
            //    (
            //        "Invalid or unset <i>Input Mode</i> setting.",
            //        $"Select a valid <i>Input Mode</i> such as {DefaultMode.ToString()} to use this input.",
            //        ValidatedHelper.MessageType.Error, this, so => FixSetMode(so, DefaultMode)
            //    );
            //    isValid = false;
            //}

            if (_inputMode == InputMode.CustomGeometryAndShader)
            {
                // Check if Renderer component is attached.
                if (!ValidatedHelper.ValidateRenderer<Renderer>(gameObject, showMessage, _checkShaderPasses, SupportsMultiPassShaders, _checkShaderName ? ShaderPrefix : String.Empty))
                {
                    isValid = false;
                }

                // Check for empty material slots
                if (_renderer != null)
                {
                    _renderer.GetSharedMaterials(_sharedMaterials);
                    for (var i = 0; i < _sharedMaterials.Count; i++)
                    {
                        // Empty material slots is a user error. Unity complains about it so we should too.
                        if (_sharedMaterials[i] == null)
                        {
                            showMessage
                            (
                                $"<i>{_renderer.GetType().Name}</i> used by this input (<i>{GetType().Name}</i>) has empty material slots.",
                                "Remove these slots or fill them with a material.",
                                ValidatedHelper.MessageType.Warning, _renderer
                            );
                        }
                    }
                }
            }

            if (_inputMode == InputMode.Painted)
            {
                if (!(this is IPaintable))
                {
                    showMessage
                    (
                        "Invalid or unset <i>Input Mode</i> setting.",
                        $"Select a valid <i>Input Mode</i> such as {DefaultMode.ToString()} to use this input.",
                        ValidatedHelper.MessageType.Error, this, so => FixSetMode(so, DefaultMode)
                    );
                    isValid = false;
                }
            }

            // Validate that any water feature required for this input is enabled, if any
            if (ocean != null)
            {
                if (!OceanRenderer.ValidateFeatureEnabled(ocean, showMessage, FeatureEnabled,
                    FeatureToggleLabel, FeatureToggleName, RequiredShaderKeyword, RequiredShaderKeywordProperty,
                    MaterialFeatureDisabledError, MaterialFeatureDisabledFix))
                {
                    isValid = false;
                }
            }

            // Suggest that if a Renderer is present, perhaps mode should be changed to use it (but only make suggestions if no errors)
            if (isValid && _inputMode != InputMode.CustomGeometryAndShader && TryGetComponent<Renderer>(out _))
            {
                showMessage
                (
                    "A <i>Renderer</i> component is present on this GameObject but will not be used by Crest.",
                    "Change the mode to <i>CustomGeometryAndShader</i> to use this renderer as the input.",
                    ValidatedHelper.MessageType.Info, this, so => FixSetMode(so, InputMode.CustomGeometryAndShader)
                );
            }

            // Suggest that if a Spline is present, perhaps mode should be changed to use it (but only make suggestions if no errors)
            if (isValid && _inputMode != InputMode.Spline && TryGetComponent<Spline.Spline>(out _))
            {
                showMessage
                (
                    "A <i>Spline</i> component is present on this GameObject but will not be used by Crest.",
                    "Change the mode to <i>Spline</i> to use this renderer as the input.",
                    ValidatedHelper.MessageType.Info, this, so => FixSetMode(so, InputMode.Spline)
                );
            }

            return isValid;
        }

        void FixSetMode(SerializedObject registerInputComponent, InputMode mode)
        {
            registerInputComponent.FindProperty("_inputMode").enumValueIndex = (int)mode;
        }
    }

    [CustomEditor(typeof(RegisterLodDataInputBase), true), CanEditMultipleObjects]
    class RegisterLodDataInputBaseEditor : PaintableEditor
    {
        public override void OnInspectorGUI()
        {
            // Show a note of what renderer we are currently using.
            var target = this.target as RegisterLodDataInputBase;
            if (target._renderer != null)
            {
                // Enable rich text in help boxes. Store original so we can revert since this might be a "hack".
                var styleRichText = GUI.skin.GetStyle("HelpBox").richText;
                GUI.skin.GetStyle("HelpBox").richText = true;
                EditorGUILayout.HelpBox($"Using renderer of type <i>{target._renderer.GetType()}</i>", MessageType.Info);
                // Revert skin since it persists.
                GUI.skin.GetStyle("HelpBox").richText = styleRichText;
            }

            base.OnInspectorGUI();
        }
    }

    public abstract partial class RegisterLodDataInputWithSplineSupport<LodDataType, SplinePointCustomData>
    {
        public override bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            bool isValid = base.Validate(ocean, showMessage);

            if (_inputMode == InputMode.Spline)
            {
                if (!TryGetComponent<Spline.Spline>(out _))
                {
                    showMessage
                    (
                        "A <i>Crest Spline</i> component is required to drive this data and none is attached to this ocean input GameObject.",
                        "Attach a <i>Crest Spline</i> component.",
                        ValidatedHelper.MessageType.Error, gameObject,
                        ValidatedHelper.FixAttachComponent<Spline.Spline>
                    );
                }

                isValid = false;
            }

            return isValid;
        }
    }
#endif
}
