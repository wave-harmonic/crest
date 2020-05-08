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
    public abstract partial class RegisterLodDataInputBase : MonoBehaviour, ILodDataInput
    {
        [SerializeField, Tooltip("Check that the shader applied to this object matches the input type (so e.g. an Animated Waves input object has an Animated Waves input shader.")]
        bool _checkShaderName = true;

        public abstract float Wavelength { get; }

        public abstract bool Enabled { get; }

        public static int sp_Weight = Shader.PropertyToID("_Weight");

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

        Renderer _renderer;
        Material[] _materials = new Material[2];

        protected virtual void Start()
        {
            _renderer = GetComponent<Renderer>();

            if (_renderer)
            {
#if UNITY_EDITOR
                if (_checkShaderName)
                {
                    CheckShaderName(_renderer, ValidatedHelper.DebugLog);
                }
#endif

                _materials[0] = _renderer.sharedMaterial;
                _materials[1] = new Material(_renderer.sharedMaterial);
            }
        }

        public void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            if (_renderer && weight > 0f)
            {
                _materials[isTransition].SetFloat(sp_Weight, weight);
                _materials[isTransition].SetInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);

                buf.DrawRenderer(_renderer, _materials[isTransition]);
            }
        }

        public int MaterialCount => _materials.Length;
        public Material GetMaterial(int index) => _materials[index];

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
    public abstract class RegisterLodDataInput<LodDataType> : RegisterLodDataInputBase
        where LodDataType : LodDataMgr
    {
        [SerializeField] bool _disableRenderer = true;

        protected abstract Color GizmoColor { get; }

        protected virtual void OnEnable()
        {
            var queue = 0;
            var rend = GetComponent<Renderer>();
            if (rend)
            {
                if (_disableRenderer)
                {
                    rend.enabled = false;
                }

                queue = (rend.sharedMaterial ?? rend.material).renderQueue;
            }

            var registrar = GetRegistrar(typeof(LodDataType));
            registrar.Add(queue, this);
        }

        protected virtual void OnDisable()
        {
            var registered = GetRegistrar(typeof(LodDataType));
            if (registered != null)
            {
                registered.Remove(this);
            }
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

#if UNITY_EDITOR
    public abstract partial class RegisterLodDataInputBase : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            return CheckShaderName(GetComponent<Renderer>(), showMessage);
        }

        bool CheckShaderName(Renderer renderer, ValidatedHelper.ShowMessage showMessage)
        {
            if (!_renderer)
            {
                showMessage
                (
                    "No renderer has been attached to ocean input. A renderer is required.",
                    ValidatedHelper.MessageType.Error, this
                );

                return false;
            }

            if (renderer.sharedMaterial && renderer.sharedMaterial.shader && !renderer.sharedMaterial.shader.name.StartsWith(ShaderPrefix))
            {
                showMessage
                (
                    $"Shader assigned to ocean input expected to be of type <i>{ShaderPrefix}</i>.",
                    ValidatedHelper.MessageType.Error, this
                );

                return false;
            }

            return true;
        }
    }

    [CustomEditor(typeof(RegisterLodDataInputBase), true), CanEditMultipleObjects]
    class RegisterLodDataInputBaseEditor : ValidatedEditor { }
#endif
}
