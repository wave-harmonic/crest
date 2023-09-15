// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Crest.Spline;
using UnityEditor;

namespace Crest
{
    using OceanInput = CrestSortedList<int, ILodDataInput>;

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

        bool IgnoreTransitionWeight { get; }
    }

    /// <summary>
    /// Base class for scripts that register input to the various LOD data types.
    /// </summary>
    [ExecuteDuringEditMode]
    public abstract partial class RegisterLodDataInputBase : CustomMonoBehaviour, ILodDataInput
    {
#pragma warning disable 414
        [SerializeField, Tooltip("Check that the shader applied to this object matches the input type (so e.g. an Animated Waves input object has an Animated Waves input shader.")]
        [Predicated(typeof(Renderer)), DecoratedField]
        bool _checkShaderName = true;

        [SerializeField, Tooltip("Check that the shader applied to this object has only a single pass as only the first pass is executed for most inputs.")]
        [Predicated(typeof(Renderer)), DecoratedField]
        bool _checkShaderPasses = true;
#pragma warning restore 414

        public const string MENU_PREFIX = Internal.Constants.MENU_SCRIPTS + "LOD Inputs/Crest Register ";

        public abstract float Wavelength { get; }

        public abstract bool Enabled { get; }

        public static int sp_Weight = Shader.PropertyToID("_Weight");
        public static int sp_DisplacementAtInputPosition = Shader.PropertyToID("_DisplacementAtInputPosition");

        // By default do not follow horizontal motion of waves. This means that the ocean input will appear on the surface at its XZ location, instead
        // of moving horizontally with the waves.
        protected virtual bool FollowHorizontalMotion => false;

        protected abstract string ShaderPrefix { get; }

        static Dictionary<Type, OceanInput> s_registrar = new Dictionary<Type, OceanInput>();

        public static OceanInput GetRegistrar(Type lodDataMgrType)
        {
            if (!s_registrar.TryGetValue(lodDataMgrType, out var registered))
            {
                registered = new OceanInput(Helpers.DuplicateComparison);
                s_registrar.Add(lodDataMgrType, registered);
            }
            return registered;
        }

        internal Renderer _renderer;
        protected Material _material;
        // We pass this to GetSharedMaterials to avoid allocations.
        protected List<Material> _sharedMaterials = new List<Material>();
        SampleHeightHelper _sampleHelper = new SampleHeightHelper();
        Vector3 _displacement;

        int _lastDrawFrame = -1;

        // If this is false, then the renderer should not be there as input source is from something else.
        protected virtual bool RendererRequired => true;
        protected virtual bool SupportsMultiPassShaders => false;

        void InitRendererAndMaterial(bool verifyShader)
        {
            if (RendererRequired && TryGetComponent(out _renderer))
            {
#if UNITY_EDITOR
                if (Application.isPlaying && verifyShader)
                {
                    ValidatedHelper.ValidateRenderer<Renderer>(gameObject, ValidatedHelper.DebugLog, _checkShaderName ? ShaderPrefix : String.Empty);
                }
#endif

                _material = _renderer.sharedMaterial;
            }
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
        }

        protected virtual void LateUpdate()
        {
            if (!FollowHorizontalMotion)
            {
                // allowMultipleCallsPerFrame because we are calling before the time values are updated.
                _sampleHelper.Init(transform.position, i_minLength: 0f, allowMultipleCallsPerFrame: true, context: this);
                _sampleHelper.Sample(out _displacement, out _, out _);
            }
            else
            {
                _displacement = Vector3.zero;
            }
        }

        public virtual void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            if (_material && weight > 0f)
            {
                buf.SetGlobalFloat(sp_Weight, weight);
                buf.SetGlobalVector(sp_DisplacementAtInputPosition, _displacement);

                if (_lastDrawFrame < OceanRenderer.FrameCount)
                {
                    _renderer.GetSharedMaterials(_sharedMaterials);
                }

                for (var i = 0; i < _sharedMaterials.Count; i++)
                {
                    Debug.AssertFormat(_sharedMaterials[i] != null, _renderer, "Crest: Attached renderer has an empty material slot which is not allowed.");

#if UNITY_EDITOR
                    // Empty material slots is a user error, but skip so we do not spam errors.
                    if (_sharedMaterials[i] == null)
                    {
                        continue;
                    }
#endif

                    // By default, shaderPass is -1 which is all passes. Shader Graph will produce multi-pass shaders
                    // for depth etc so we should only render one pass. Unlit SG will have the unlit pass first.
                    // Submesh count generally must equal number of materials.
                    buf.DrawRenderer(_renderer, _sharedMaterials[i], submeshIndex: i, shaderPass: 0);
                }

                _lastDrawFrame = OceanRenderer.FrameCount;
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

        public bool IgnoreTransitionWeight => false;
    }

    /// <summary>
    /// Registers input to a particular LOD data.
    /// </summary>
    public abstract partial class RegisterLodDataInput<LodDataType> : RegisterLodDataInputBase
        where LodDataType : LodDataMgr
    {
        protected const string k_displacementCorrectionTooltip = "Whether this input data should displace horizontally with waves. If false, data will not move from side to side with the waves. Adds a small performance overhead when disabled.";

        [SerializeField, Predicated(typeof(Renderer)), DecoratedField]
        bool _disableRenderer = true;

        protected abstract Color GizmoColor { get; }

        int _registeredQueueValue = int.MinValue;

        protected virtual bool GetQueue(out int queue)
        {
            if (TryGetComponent<Renderer>(out var rend) && rend.sharedMaterial != null)
            {
                queue = rend.sharedMaterial.renderQueue;
                return true;
            }
            queue = int.MinValue;
            return false;
        }

        public static void RegisterInput(ILodDataInput input, int queueSortIndex, int subSortIndex)
        {
            var registrar = GetRegistrar(typeof(LodDataType));
            registrar.Remove(input);

            // Allow sorting within a queue. Callers can pass in things like sibling index to get deterministic sorting
            int maxSubIndex = 1000;
            int finalSortIndex = queueSortIndex * maxSubIndex + Mathf.Min(subSortIndex, maxSubIndex - 1);

            registrar.Add(finalSortIndex, input);
        }

        public static void DeregisterInput(ILodDataInput input)
        {
            var registrar = GetRegistrar(typeof(LodDataType));
            registrar.Remove(input);
        }

        protected virtual void OnEnable()
        {
            if (_disableRenderer)
            {
                if (TryGetComponent<Renderer>(out var rend))
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
            RegisterInput(this, q, transform.GetSiblingIndex());
            _registeredQueueValue = q;
        }

        protected virtual void OnDisable()
        {
            DeregisterInput(this);
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
                        RegisterInput(this, q, transform.GetSiblingIndex());
                        _registeredQueueValue = q;
                    }
                }
            }
#endif
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (TryGetComponent<MeshFilter>(out var mf))
            {
                Gizmos.color = GizmoColor;
                Gizmos.DrawWireMesh(mf.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
            }
        }
    }

    public abstract class RegisterLodDataInputWithSplineSupport<LodDataType>
        : RegisterLodDataInputWithSplineSupport<LodDataType, SplinePointDataNone>
        where LodDataType : LodDataMgr
    {
    }

    public abstract partial class RegisterLodDataInputWithSplineSupport<LodDataType, SplinePointCustomData>
        : RegisterLodDataInput<LodDataType>
        , ISplinePointCustomDataSetup
#if UNITY_EDITOR
        , IReceiveSplinePointOnDrawGizmosSelectedMessages
#endif
        where LodDataType : LodDataMgr
        where SplinePointCustomData : CustomMonoBehaviour, ISplinePointCustomData
    {
        [Header("Spline settings")]
        [SerializeField, Predicated(typeof(Spline.Spline)), DecoratedField]
        bool _overrideSplineSettings = false;
        [SerializeField, Predicated("_overrideSplineSettings", typeof(Spline.Spline)), DecoratedField]
        float _radius = 20f;
        [SerializeField, Predicated("_overrideSplineSettings", typeof(Spline.Spline)), Delayed]
        int _subdivisions = 1;

        protected Material _splineMaterial;
        Spline.Spline _spline;
        Mesh _splineMesh;
        protected Vector3[] _splineBoundingPoints = new Vector3[0];

        protected abstract string SplineShaderName { get; }
        protected abstract Vector2 DefaultCustomData { get; }

        protected override bool RendererRequired => !TryGetComponent<Spline.Spline>(out _);

        protected float _splinePointHeightMin;
        protected float _splinePointHeightMax;

        protected override bool FollowHorizontalMotion => _spline != null;

        void Awake()
        {
            CreateOrUpdateSplineMesh();
        }

        void CreateOrUpdateSplineMesh()
        {
            if (_spline == null && !TryGetComponent(out _spline))
            {
                _splineMesh = null;
                return;
            }

            var radius = _overrideSplineSettings ? _radius : _spline.Radius;
            var subdivs = _overrideSplineSettings ? _subdivisions : _spline.Subdivisions;
            ShapeGerstnerSplineHandling.GenerateMeshFromSpline<SplinePointCustomData>(_spline, transform, subdivs,
                radius, DefaultCustomData, ref _splineMesh, out _splinePointHeightMin, out _splinePointHeightMax, ref _splineBoundingPoints);

            if (_splineMaterial == null)
            {
                _splineMaterial = new Material(Shader.Find(SplineShaderName));
            }
        }

        protected virtual void CreateSplineMaterial()
        {
            _splineMaterial = new Material(Shader.Find(SplineShaderName));
        }

        public override void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            if (weight <= 0f) return;

            if (_splineMesh != null && _splineMaterial != null)
            {
                buf.SetGlobalFloat(sp_Weight, weight);
                buf.SetGlobalVector(sp_DisplacementAtInputPosition, Vector3.zero);
                buf.DrawMesh(_splineMesh, transform.localToWorldMatrix, _splineMaterial);
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
        protected override void OnDrawGizmosSelected()
        {
            if (!TryGetComponent(out _spline))
            {
                base.OnDrawGizmosSelected();
                return;
            }

            // Restrict this call as it is costly.
            if (Selection.activeGameObject == gameObject)
            {
                CreateOrUpdateSplineMesh();
            }

            Gizmos.color = GizmoColor;
            Gizmos.DrawWireMesh(_splineMesh, transform.position, transform.rotation, transform.lossyScale);
        }

        public void OnSplinePointDrawGizmosSelected(SplinePoint point)
        {
            CreateOrUpdateSplineMesh();
            OnDrawGizmosSelected();
        }
#endif // UNITY_EDITOR
    }

#if UNITY_EDITOR
    public abstract partial class RegisterLodDataInputBase : IValidated
    {
        // Whether there is an alternative methods than a renderer (like splines).
        protected virtual bool RendererOptional => false;

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
            var isValid = ValidatedHelper.ValidateRenderer<Renderer>(gameObject, showMessage, RendererRequired, RendererOptional, _checkShaderName ? ShaderPrefix : String.Empty);

            if (_checkShaderPasses && _material != null && _material.passCount > 1 && !SupportsMultiPassShaders)
            {
                showMessage
                (
                    $"The shader <i>{_material.shader.name}</i> for material <i>{_material.name}</i> has multiple passes which might not work as expected as only the first pass is executed. " +
                    "See documentation for more information on what multi-pass shaders work or",
                    "use a shader with a single pass.",
                    ValidatedHelper.MessageType.Warning, this
                );
            }

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
                            ValidatedHelper.MessageType.Error, _renderer
                        );
                    }
                }
            }

            if (ocean != null && !FeatureEnabled(ocean))
            {
                showMessage($"<i>{FeatureToggleLabel}</i> must be enabled on the <i>OceanRenderer</i> component.",
                    $"Enable the <i>{FeatureToggleLabel}</i> option on the <i>OceanRenderer</i> component.",
                    ValidatedHelper.MessageType.Error, ocean,
                    (so) => OceanRenderer.FixSetFeatureEnabled(so, FeatureToggleName, true)
                    );
                isValid = false;
            }

            if (ocean != null && !string.IsNullOrEmpty(RequiredShaderKeyword) && ocean.OceanMaterial.HasProperty(RequiredShaderKeywordProperty) && !ocean.OceanMaterial.IsKeywordEnabled(RequiredShaderKeyword))
            {
                showMessage(MaterialFeatureDisabledError, MaterialFeatureDisabledFix,
                    ValidatedHelper.MessageType.Error, ocean.OceanMaterial,
                    (material) => ValidatedHelper.FixSetMaterialOptionEnabled(material, RequiredShaderKeyword, RequiredShaderKeywordProperty, true));
                isValid = false;
            }

            return isValid;
        }
    }

    [CustomEditor(typeof(RegisterLodDataInputBase), true), CanEditMultipleObjects]
    class RegisterLodDataInputBaseEditor : CustomBaseEditor
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

    public abstract partial class RegisterLodDataInput<LodDataType>
    {
        public override bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = base.Validate(ocean, showMessage);

            // If we have a renderer then validate the layer.
            if (RendererRequired && TryGetComponent<Renderer>(out _) && !_disableRenderer)
            {
                ValidatedHelper.ValidateRendererLayer(gameObject, showMessage, ocean);
            }

            return isValid;
        }
    }

    public abstract partial class RegisterLodDataInputWithSplineSupport<LodDataType, SplinePointCustomData>
    {
        protected override bool RendererOptional => true;

        public override bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            bool isValid = base.Validate(ocean, showMessage);

            if (RendererRequired && !TryGetComponent<Renderer>(out _))
            {
                showMessage
                (
                    "A <i>Crest Spline</i> component is required to drive this data. Alternatively a <i>Renderer</i> can be added. Neither is currently attached to ocean input.",
                    "Attach a <i>Crest Spline</i> component.",
                    ValidatedHelper.MessageType.Error, gameObject,
                    ValidatedHelper.FixAttachComponent<Spline.Spline>
                );
            }

            return isValid;
        }
    }
#endif
}
