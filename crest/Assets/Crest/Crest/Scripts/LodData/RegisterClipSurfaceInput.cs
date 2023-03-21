// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the clip surface simulation. Attach this to GameObjects that you want to use to
    /// clip the surface of the ocean.
    /// </summary>
    [AddComponentMenu(MENU_PREFIX + "Clip Surface Input")]
    [CrestHelpURL("user/clipping", "clip-surface")]
    [FilterEnum("_inputMode", FilteredAttribute.Mode.Exclude, (int)InputMode.Spline)]
    public partial class RegisterClipSurfaceInput : RegisterLodDataInput<LodDataMgrClipSurface>, IPaintable
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

        // Have this match UnityEngine.PrimitiveType.
        public enum Primitive
        {
            Sphere = 0,
            Cube = 3,
        }

        bool _enabled = true;
        public override bool Enabled => _enabled;

        // Primitive is the best default for clipping, so override the default defined in the base class.
        public override InputMode DefaultMode => InputMode.Primitive;

        [Heading("Primitive Mode Settings")]

        [Tooltip("The primitive to render (signed distance) into the simulation.")]
        [SerializeField, Predicated("_inputMode", inverted: true, InputMode.Primitive, hide: true), DecoratedField]
        Primitive _primitive = Primitive.Cube;

        // Only needed for Primitive as non-primitive uses queue from shader.
        [Tooltip("Order (ascending) that this input will be rendered into the clip surface data.")]
        [SerializeField, Predicated("_inputMode", inverted: true, InputMode.Primitive, hide: true), DecoratedField]
        int _order = 0;

        // Only Mode.Primitive SDF supports inverted.
        [Tooltip("Removes clip surface data instead of adding it.")]
        [SerializeField, Predicated("_inputMode", inverted: true, InputMode.Primitive, hide: true), DecoratedField]
        bool _inverted = false;

        [Heading("3D Clipping Options")]

        [Tooltip("Prevents inputs from cancelling each other out when aligned vertically. It is imperfect so custom logic might be needed for your use case.")]
        [SerializeField, Predicated("_inputMode", inverted: false, InputMode.Painted, hide: true), Predicated("_inputMode", inverted: true, InputMode.CustomGeometryAndShader), DecoratedField]
        bool _disableClipSurfaceWhenTooFarFromSurface = false;

        [Tooltip("Large, choppy waves require higher iterations to have accurate holes.")]
        [SerializeField, Predicated("_inputMode", inverted: false, InputMode.Painted, hide: true), DecoratedField]
        uint _animatedWavesDisplacementSamplingIterations = 4;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(0f, 1f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Clip Surface";

        // The clip surface samples at the displaced position in the ocean shader, so the displacement correction is not needed.
        protected override bool FollowHorizontalMotion => true;

        #region Painting
        [Heading("Paint Mode Settings")]
        [Predicated("_inputMode", inverted: true, InputMode.Painted, hide: true), DecoratedField]
        public CPUTexture2DPaintable_R16_AddBlend _paintData;
        public IPaintedData PaintedData => _paintData;
        public Shader PaintedInputShader => Shader.Find("Hidden/Crest/Inputs/Clip Surface/Painted");

        protected override void PreparePaintInputMaterial(Material mat)
        {
            base.PreparePaintInputMaterial(mat);
            if (_paintData == null) return;

            _paintData.CenterPosition3 = transform.position;
            _paintData.PrepareMaterial(mat, CPUTexture2DHelpers.ColorConstructFnOneChannel);
        }

        protected override void UpdatePaintInputMaterial(Material mat)
        {
            base.UpdatePaintInputMaterial(mat);
            if (_paintData == null) return;

            _paintData.CenterPosition3 = transform.position;
            _paintData.UpdateMaterial(mat, CPUTexture2DHelpers.ColorConstructFnOneChannel);
        }

        public void ClearData() => _paintData.Clear(this, 0f);
        public void MakeDirty() => _paintData.MakeDirty();

        public bool Paint(Vector3 paintPosition3, Vector2 paintDir, float paintWeight, bool remove)
        {
            _paintData.CenterPosition3 = transform.position;

            return _paintData.PaintSmoothstep(this, paintPosition3, paintWeight, remove ? -1f : 1f, _paintData.BrushRadius, _paintData._brushStrength, CPUTexturePaintHelpers.PaintFnAdditiveBlendSaturateFloat, remove);
        }
        #endregion

        // Cache shader name to prevent allocations.
        protected override bool SupportsMultiPassShaders => _currentShaderName == "Crest/Inputs/Clip Surface/Convex Hull";
        Material _currentMaterial;
        string _currentShaderName;

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
            if (_inputMode == InputMode.Primitive)
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

        public override void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            if (weight <= 0f)
            {
                return;
            }

            if (_inputMode == InputMode.Unset)
            {
                Debug.LogError($"Crest: {GetType().Name} has component does not have an Input Mode set, please set this to a supported option such as {DefaultMode}. Click this message to highlight the relevant GameObject.", this);
                return;
            }

            buf.SetGlobalFloat(sp_Weight, weight);
            buf.SetGlobalFloat(LodDataMgr.sp_LD_SliceIndex, lodIdx);
            buf.SetGlobalVector(sp_DisplacementAtInputPosition, Vector3.zero);

            if (_inputMode == InputMode.Painted)
            {
                if (_paintInputMaterial != null)
                {
                    buf.DrawProcedural(Matrix4x4.identity, _paintInputMaterial, 0, MeshTopology.Triangles, 3);
                }
            }
            else if (_inputMode == InputMode.Primitive)
            {
                if (_signedDistancedMaterial == null)
                {
                    return;
                }

                // Need this here or will see NullReferenceException on recompile.
                if (_mpb == null)
                {
                    _mpb = new PropertyWrapperMPB();
                }

                buf.DrawMesh(QuadMesh, QuadMatrix, _signedDistancedMaterial, submeshIndex: 0, shaderPass: 0, _mpb.materialPropertyBlock);
            }
            else if (_inputMode == InputMode.CustomGeometryAndShader)
            {
                if (_renderer != null)
                {
                    var shaderPass = SupportsMultiPassShaders ? -1 : 0;
                    _renderer.GetSharedMaterials(_sharedMaterials);
                    for (var i = 0; i < _sharedMaterials.Count; i++)
                    {
                        // Empty material slots is a user error, but skip so we do not spam errors.
                        if (_sharedMaterials[i] == null)
                        {
                            continue;
                        }

                        buf.DrawRenderer(_renderer, _sharedMaterials[i], submeshIndex: i, shaderPass);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (OceanRenderer.Instance == null || (_inputMode == InputMode.CustomGeometryAndShader && _renderer == null))
            {
                return;
            }

            if (_renderer != null && _currentMaterial != _renderer.sharedMaterial)
            {
                _currentMaterial = _renderer.sharedMaterial;
                // GC allocation hence the caching.
                _currentShaderName = _renderer.sharedMaterial.name;
            }

            // Prevents possible conflicts since overlapping doesn't work for every case for convex null.
            if (_inputMode == InputMode.CustomGeometryAndShader && _disableClipSurfaceWhenTooFarFromSurface)
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

                if (_inputMode == InputMode.CustomGeometryAndShader)
                {
                    _renderer.GetPropertyBlock(_mpb.materialPropertyBlock);
                }
                else
                {
                    _signedDistancedMaterial.SetKeyword("_INVERTED", _inverted);
                    _signedDistancedMaterial.SetInt(sp_BlendOp, (int)(_inverted ? BlendOp.Min : BlendOp.Max));
                }

                _mpb.SetInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                _mpb.SetInt(sp_DisplacementSamplingIterations, (int)_animatedWavesDisplacementSamplingIterations);

                if (_inputMode == InputMode.CustomGeometryAndShader)
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

        // Use Unity's UV sphere mesh for gizmos as Gizmos.DrawSphere is too low resolution.
        static Mesh s_SphereMesh;

        protected new void OnDrawGizmosSelected()
        {
            Gizmos.color = GizmoColor;

            if (_inputMode == InputMode.Painted)
            {
                base.OnDrawGizmosSelected();
                return;
            }

            if (_inputMode == InputMode.CustomGeometryAndShader)
            {
                if (TryGetComponent<MeshFilter>(out var mf))
                {
                    Gizmos.DrawWireMesh(mf.sharedMesh, 0, transform.position, transform.rotation, transform.lossyScale);
                }

                return;
            }

            if (_inputMode == InputMode.Primitive)
            {
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
                _inputMode = InputMode.CustomGeometryAndShader;

                _version = 1;
            }
        }
    }

#if UNITY_EDITOR
    // Ensure preview works (preview does not apply to derived classes so done per type)
    [CustomPreview(typeof(RegisterClipSurfaceInput))]
    public class RegisterClipSurfaceInputPreview : UserPaintedDataPreview
    {
    }
#endif // UNITY_EDITOR
}
