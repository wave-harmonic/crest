// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the clip surface simulation. Attach this to GameObjects that you want to use to
    /// clip the surface of the ocean.
    /// </summary>
    [AddComponentMenu(MENU_PREFIX + "Clip Surface Input")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "ocean-simulation.html" + Internal.Constants.HELP_URL_RP + "#clip-surface")]
    public partial class RegisterClipSurfaceInput : RegisterLodDataInput<LodDataMgrClipSurface>
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 1;
#pragma warning restore 414

        const string k_SignedDistanceShaderPath = "Hidden/Crest/Inputs/Clip Surface/Signed Distance";

        public enum Mode
        {
            Geometry,
            Primitive,
        }

        // Have this match UnityEngine.PrimitiveType.
        public enum Primitive
        {
            Sphere = 0,
            Cube = 3,
        }

        bool _enabled = true;
        public override bool Enabled => _enabled;

        [Header("Clip Surface Input Options")]

        [Tooltip("Where the source of the clipping will come from.")]
        [SerializeField]
        internal Mode _mode = Mode.Primitive;

        [Tooltip("The primitive to render (signed distance) into the simulation.")]
        [SerializeField, Predicated("_mode", inverted: true, Mode.Primitive), DecoratedField]
        Primitive _primitive = Primitive.Cube;

        [Tooltip("Order (ascending) that this input will be rendered into the clip surface data.")]
        [SerializeField, Predicated("_mode", inverted: true, Mode.Primitive), DecoratedField]
        int _order = 0;

        [Tooltip("Removes clip surface data instead of adding it.")]
        [SerializeField, Predicated("_mode", inverted: true, Mode.Primitive), DecoratedField]
        bool _inverted = false;

        [Header("3D Clipping Options")]

        [Tooltip("Prevents inputs from cancelling each other out when aligned vertically. It is imperfect so custom logic might be needed for your use case.")]
        [SerializeField] bool _disableClipSurfaceWhenTooFarFromSurface = false;

        [Tooltip("Large, choppy waves require higher iterations to have accurate holes.")]
        [SerializeField] uint _animatedWavesDisplacementSamplingIterations = 4;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(0f, 1f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Clip Surface";

        // The clip surface samples at the displaced position in the ocean shader, so the displacement correction is not needed.
        protected override bool FollowHorizontalMotion => true;

        PropertyWrapperMPB _mpb;
        SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

        static int sp_DisplacementSamplingIterations = Shader.PropertyToID("_DisplacementSamplingIterations");
        static readonly int sp_SignedDistanceShapeMatrix = Shader.PropertyToID("_SignedDistanceShapeMatrix");
        static readonly int sp_BlendOp = Shader.PropertyToID("_BlendOp");

        Material _signedDistancedMaterial;
        Primitive _activePrimitive;

        // For rendering signed distance shapes and gizmos.
        Matrix4x4 QuadMatrix
        {
            get
            {
                var position = transform.position;
                // Apply sea level to matrix so we can use it for rendering and gizmos.
                position.y = OceanRenderer.Instance.SeaLevel;
                var scale = Vector3.one * (Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) * 2f);
                scale.z = 0f;
                return Matrix4x4.TRS(position, Quaternion.Euler(90f, 0f, 0f), scale);
            }
        }

        protected override void Start()
        {
            base.Start();

            InitializeSignedDistanceMaterial();
        }

        protected override void Update()
        {
            base.Update();

#if UNITY_EDITOR
            InitializeSignedDistanceMaterial();
#endif
        }

        protected override bool GetQueue(out int queue)
        {
            // Support queue for primitives.
            if (_mode == Mode.Primitive)
            {
                queue = _order;
                return true;
            }
            else
            {
                return base.GetQueue(out queue);
            }
        }

        void InitializeSignedDistanceMaterial()
        {
            if (_signedDistancedMaterial == null)
            {
                _signedDistancedMaterial = new Material(Shader.Find(k_SignedDistanceShaderPath));
                _signedDistancedMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            // Could refactor using hashy.
            if (_primitive != _activePrimitive)
            {
                foreach (var primitive in System.Enum.GetNames(typeof(Primitive)))
                {
                    _signedDistancedMaterial.DisableKeyword($"_{primitive.ToUpper()}");
                }

                _signedDistancedMaterial.EnableKeyword($"_{System.Enum.GetName(typeof(Primitive), _primitive).ToUpper()}");

                _activePrimitive = _primitive;
            }
        }

        public override void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            if (weight <= 0f)
            {
                return;
            }

            if (_mode == Mode.Primitive && _signedDistancedMaterial == null)
            {
                return;
            }

            if (_mode == Mode.Geometry && (_renderer == null || _material == null))
            {
                return;
            }

            buf.SetGlobalFloat(sp_Weight, weight);
            buf.SetGlobalFloat(LodDataMgr.sp_LD_SliceIndex, lodIdx);
            buf.SetGlobalVector(sp_DisplacementAtInputPosition, Vector3.zero);

            if (_mode == Mode.Primitive)
            {
                // Need this here or will see NullReferenceException on recompile.
                if (_mpb == null)
                {
                    _mpb = new PropertyWrapperMPB();
                }

                buf.DrawMesh(QuadMesh, QuadMatrix, _signedDistancedMaterial, submeshIndex: 0, shaderPass: 0, _mpb.materialPropertyBlock);
            }
            else
            {
                buf.DrawRenderer(_renderer, _material);
            }
        }

        private void LateUpdate()
        {
            if (OceanRenderer.Instance == null || (_mode == Mode.Geometry && _renderer == null))
            {
                return;
            }

            // Prevents possible conflicts since overlapping doesn't work for every case for convex null.
            if (_disableClipSurfaceWhenTooFarFromSurface)
            {
                var position = transform.position;
                _sampleHeightHelper.Init(position, 0f);

                if (_sampleHeightHelper.Sample(out float waterHeight))
                {
                    position.y = waterHeight;
                    _enabled = Mathf.Abs(_renderer.bounds.ClosestPoint(position).y - waterHeight) < 1;
                }
            }
            else
            {
                _enabled = true;
            }

            // find which lod this object is overlapping
            var rect = new Rect(transform.position.x, transform.position.z, 0f, 0f);
            var lodIdx = LodDataMgrAnimWaves.SuggestDataLOD(rect);

            if (lodIdx > -1)
            {
                // Need this here or will see NullReferenceException on recompile.
                if (_mpb == null)
                {
                    _mpb = new PropertyWrapperMPB();
                }

                if (_mode == Mode.Geometry)
                {
                    _renderer.GetPropertyBlock(_mpb.materialPropertyBlock);
                }
                else
                {
                    if (_inverted)
                    {
                        _signedDistancedMaterial.EnableKeyword("_INVERTED");
                    }
                    else
                    {
                        _signedDistancedMaterial.DisableKeyword("_INVERTED");
                    }

                    _signedDistancedMaterial.SetInt(sp_BlendOp, (int)(_inverted ? BlendOp.Min : BlendOp.Max));
                }

                _mpb.SetInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                _mpb.SetInt(sp_DisplacementSamplingIterations, (int)_animatedWavesDisplacementSamplingIterations);

                if (_mode == Mode.Geometry)
                {
                    _renderer.SetPropertyBlock(_mpb.materialPropertyBlock);
                }
                else
                {
                    _mpb.SetMatrix(sp_SignedDistanceShapeMatrix, Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale).inverse);
                }
            }
        }

#if UNITY_EDITOR
        protected override string FeatureToggleName => "_createClipSurfaceData";
        protected override string FeatureToggleLabel => "Create Clip Surface Data";
        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateClipSurfaceData;
        protected override string RequiredShaderKeywordProperty => LodDataMgrClipSurface.MATERIAL_KEYWORD_PROPERTY;
        protected override string RequiredShaderKeyword => LodDataMgrClipSurface.MATERIAL_KEYWORD;
        protected override string MaterialFeatureDisabledError => LodDataMgrClipSurface.ERROR_MATERIAL_KEYWORD_MISSING;
        protected override string MaterialFeatureDisabledFix => LodDataMgrClipSurface.ERROR_MATERIAL_KEYWORD_MISSING_FIX;

        protected override bool RendererRequired => _mode == Mode.Geometry;
        protected override bool RendererOptional => _mode != Mode.Geometry;

        // Use Unity's UV sphere mesh for gizmos as Gizmos.DrawSphere is too low resolution.
        static Mesh s_SphereMesh;

        protected new void OnDrawGizmosSelected()
        {
            Gizmos.color = GizmoColor;

            if (_mode == Mode.Geometry)
            {
                if (TryGetComponent<MeshFilter>(out var mf))
                {
                    Gizmos.DrawWireMesh(mf.sharedMesh, 0, transform.position, transform.rotation, transform.lossyScale);
                }

                return;
            }

            // Show gizmo for quad which encompasses the shape.
            Gizmos.matrix = QuadMatrix;
            Gizmos.DrawWireMesh(QuadMesh);

            Gizmos.matrix = transform.localToWorldMatrix;

            switch (_primitive)
            {
                case Primitive.Sphere:
                    if (s_SphereMesh == null)
                    {
                        s_SphereMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
                    }

                    // Render mesh and wire sphere at default size (0.5m radius) which is scaled by gizmo matrix.
                    Gizmos.DrawMesh(s_SphereMesh, submeshIndex: 0, Vector3.zero, Quaternion.identity, Vector3.one);
                    Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
                    break;
                case Primitive.Cube:
                    // Render mesh and wire box at default size which is scaled by gizmo matrix.
                    Gizmos.DrawCube(Vector3.zero, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    break;
                default:
                    Debug.LogError("Crest: Not a valid primitive type!");
                    break;
            }
        }
#endif
    }

    // Version handling - perform data migration after data loaded.
    public partial class RegisterClipSurfaceInput : ISerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
            // Intentionally left empty.
        }

        public void OnAfterDeserialize()
        {
            // Version 1 (2021.07.25)
            // - default mode changed from geo to primitive
            if (_version == 0)
            {
                // The user is using geometry for clipping.
                _mode = Mode.Geometry;

                _version = 1;
            }
        }
    }
}
