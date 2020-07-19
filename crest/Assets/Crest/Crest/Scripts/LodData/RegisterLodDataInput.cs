// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx);
        float Wavelength { get; }
        bool Enabled { get; }
    }

    /// <summary>
    /// Base class for scripts that register input to the various LOD data types.
    /// </summary>
    [ExecuteAlways]
    public abstract partial class RegisterLodDataInputBase : MonoBehaviour, ILodDataInput
    {
#if UNITY_EDITOR
        [SerializeField, Tooltip("Check that the shader applied to this object matches the input type (so e.g. an Animated Waves input object has an Animated Waves input shader.")]
        bool _checkShaderName = true;
#endif

        public abstract float Wavelength { get; }

        public abstract bool Enabled { get; }

        public static int sp_Weight = Shader.PropertyToID("_Weight");
        public static int sp_DisplacementAtInputPosition = Shader.PropertyToID("_DisplacementAtInputPosition");

        // By default do not follow horizontal motion of waves. This means that the ocean input will appear on the surface at its XZ location, instead
        // of moving horizontally with the waves.
        protected virtual bool FollowHorizontalMotion => false;

        protected abstract string ShaderPrefix { get; }

        static DuplicateKeyComparer<int> s_comparer = new DuplicateKeyComparer<int>();
        static Dictionary<Type, OceanInput> s_registrar = new Dictionary<Type, OceanInput>();

        public static OceanInput GetRegistrar(Type lodDataMgrType)
        {
            OceanInput registered;
            if (!s_registrar.TryGetValue(lodDataMgrType, out registered))
            {
                registered = new OceanInput(s_comparer);
                s_registrar.Add(lodDataMgrType, registered);
            }
            return registered;
        }

        protected Renderer _renderer;
        protected Material _material;
        SampleHeightHelper _sampleHelper = new SampleHeightHelper();

        void InitRendererAndMaterial(bool verifyShader)
        {
            _renderer = GetComponent<Renderer>();

            if (_renderer)
            {
#if UNITY_EDITOR
                if (Application.isPlaying && _checkShaderName && verifyShader)
                {
                    ValidatedHelper.ValidateRenderer(gameObject, ShaderPrefix, ValidatedHelper.DebugLog);
                }
#endif

                _material = _renderer.sharedMaterial;
            }
        }

        protected void Start()
        {
            InitRendererAndMaterial(true);
        }

        protected virtual void Update()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                InitRendererAndMaterial(true);
            }
#endif
        }

        public void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            if (_renderer && _material && weight > 0f)
            {
                buf.SetGlobalFloat(sp_Weight, weight);
                buf.SetGlobalFloat(LodDataMgr.sp_LD_SliceIndex, lodIdx);

                if (!FollowHorizontalMotion)
                {
                    // This can be called multiple times per frame - one for each LOD potentially
                    _sampleHelper.Init(transform.position, 0f, true, this);
                    _sampleHelper.Sample(out Vector3 displacement, out _, out _);
                    _material.SetVector(sp_DisplacementAtInputPosition, displacement);
                }
                else
                {
                    _material.SetVector(sp_DisplacementAtInputPosition, Vector3.zero);
                }

                buf.DrawRenderer(_renderer, _material);
            }
        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_registrar.Clear();
            sp_Weight = Shader.PropertyToID("_Weight");
        }
    }

    /// <summary>
    /// Registers input to a particular LOD data.
    /// </summary>
    [ExecuteAlways]
    public abstract class RegisterLodDataInput<LodDataType> : RegisterLodDataInputBase
        where LodDataType : LodDataMgr
    {
        [SerializeField] bool _disableRenderer = true;

        protected abstract Color GizmoColor { get; }

        int _registeredQueueValue = int.MinValue;

        bool GetQueue(out int queue)
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

        protected virtual void OnEnable()
        {
            if (_disableRenderer)
            {
                var rend = GetComponent<Renderer>();
                if (rend)
                {
                    rend.enabled = false;
                }
            }

            int q;
            GetQueue(out q);

            var registrar = GetRegistrar(typeof(LodDataType));
            registrar.Add(q, this);
            _registeredQueueValue = q;
        }

        protected virtual void OnDisable()
        {
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
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                int q;
                if (GetQueue(out q))
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

        private void OnDrawGizmosSelected()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf)
            {
                Gizmos.color = GizmoColor;
                Gizmos.DrawWireMesh(mf.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
            }
        }
    }

    [ExecuteAlways]
    public abstract class RegisterLodDataInputDisplacementCorrection<LodDataType> : RegisterLodDataInput<LodDataType>
        where LodDataType : LodDataMgr
    {
        [SerializeField, Tooltip("Correct for horizontal displacement so that this input does not move from side to side with the waves. Adds a small performance overhead when disabled.")]
        bool _followHorizontalMotion = false;

        protected override bool FollowHorizontalMotion => _followHorizontalMotion;
    }

#if UNITY_EDITOR
    public abstract partial class RegisterLodDataInputBase : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            return ValidatedHelper.ValidateRenderer(gameObject, ShaderPrefix, showMessage);
        }
    }

    [CustomEditor(typeof(RegisterLodDataInputBase), true), CanEditMultipleObjects]
    class RegisterLodDataInputBaseEditor : ValidatedEditor { }
#endif
}
