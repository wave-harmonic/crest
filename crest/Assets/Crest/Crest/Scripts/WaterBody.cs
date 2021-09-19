// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Demarcates an AABB area where water is present in the world. If present, ocean tiles will be
    /// culled if they don't overlap any WaterBody.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Water Body")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "water-bodies.html")]
    public partial class WaterBody : MonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Tooltip("Editor only: run validation checks on Start() to check for issues."), SerializeField]
#pragma warning disable 414
        bool _runValidationOnStart = true;
#pragma warning restore 414

        public static List<WaterBody> WaterBodies => _waterBodies;
        static List<WaterBody> _waterBodies = new List<WaterBody>();

        public Bounds AABB { get; private set; }

        [Tooltip("Water geometry tiles that overlap this waterbody area will be assigned this material. This " +
            "is useful for varying water appearance across different water bodies. If no override material is " +
            "specified, the default material assigned to the OceanRenderer component will be used.")]
        public Material _overrideMaterial = null;

        class ClipInput : ILodDataInput
        {
            Material _renderMat;

            // Render to all cascades
            public float Wavelength => 0f;
            public bool Enabled => true;

            public Matrix4x4 _transform;

            public ClipInput(WaterBody owner)
            {
                var rotateQuadFaceUp = Matrix4x4.Rotate(Quaternion.AngleAxis(90, Vector3.right));
                _transform = owner.transform.localToWorldMatrix * rotateQuadFaceUp;

                _renderMat = new Material(Shader.Find("Crest/Inputs/Clip Surface/Include Area"));
            }

            public void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
            {
                buf.DrawMesh(RegisterLodDataInputBase.QuadMesh, _transform, _renderMat);
            }
        }

        ClipInput _clipInput;

        private void OnEnable()
        {
            CalculateBounds();

            _waterBodies.Add(this);

            if (OceanRenderer.Instance && OceanRenderer.Instance.CreateClipSurfaceData
                && OceanRenderer.Instance._defaultClippingState == OceanRenderer.DefaultClippingState.EverythingClipped)
            {
                if (_clipInput == null)
                {
                    _clipInput = new ClipInput(this);
                }

                var registrar = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrClipSurface));
                registrar.Remove(_clipInput);
                registrar.Add(0, _clipInput);
            }
        }

        private void OnDisable()
        {
            _waterBodies.Remove(this);

            if (_clipInput != null)
            {
                RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrClipSurface)).Remove(_clipInput);
            }
        }

        private void CalculateBounds()
        {
            var bounds = new Bounds();
            bounds.center = transform.position;
            bounds.Encapsulate(transform.TransformPoint(Vector3.right / 2f + Vector3.forward / 2f));
            bounds.Encapsulate(transform.TransformPoint(Vector3.right / 2f - Vector3.forward / 2f));
            bounds.Encapsulate(transform.TransformPoint(-Vector3.right / 2f + Vector3.forward / 2f));
            bounds.Encapsulate(transform.TransformPoint(-Vector3.right / 2f - Vector3.forward / 2f));

            AABB = bounds;
        }

#if UNITY_EDITOR
        private void Start()
        {
            if (EditorApplication.isPlaying && _runValidationOnStart)
            {
                Validate(OceanRenderer.Instance, ValidatedHelper.DebugLog);
            }
        }

        private void Update()
        {
            if (_clipInput != null)
            {
                var rotateQuadFaceUp = Matrix4x4.Rotate(Quaternion.AngleAxis(90, Vector3.right));
                _clipInput._transform = transform.localToWorldMatrix * rotateQuadFaceUp;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Required as we're not normally executing in edit mode
            CalculateBounds();

            var oldColor = Gizmos.color;
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            var center = AABB.center;
            Gizmos.DrawCube(center, 2f * new Vector3(AABB.extents.x, 1f, AABB.extents.z));
            Gizmos.color = oldColor;
        }
#endif
    }

#if UNITY_EDITOR
    public partial class WaterBody : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            // This will also return disabled objects. Safe to use in this case.
            if (Resources.FindObjectsOfTypeAll<OceanRenderer>().Length == 0)
            {
                showMessage
                (
                    $"Water body <i>{gameObject.name}</i> requires an ocean renderer component to be present.",
                    "Create a separate GameObject and add an <i>OceanRenderer</i> component to it.",
                    ValidatedHelper.MessageType.Error, this
                );

                return false;
            }

            if (Mathf.Abs(transform.lossyScale.x) < 2f && Mathf.Abs(transform.lossyScale.z) < 2f)
            {
                showMessage
                (
                    $"Water body {gameObject.name} has a very small size (the size is set by the X & Z scale of its transform), and will be a very small body of water.",
                    "Increase X & Z scale on water body transform (or parents).",
                    ValidatedHelper.MessageType.Error, this
                );

                return false;
            }

            if (transform.eulerAngles.magnitude > 0.0001f)
            {
                showMessage
                (
                    $"There must be no rotation on the water body GameObject, and no rotation on any parent. Currently the rotation Euler angles are {transform.eulerAngles}.",
                    "Reset the rotations on this GameObject and all parents to 0.",
                    ValidatedHelper.MessageType.Error, this
                );
            }

            return true;
        }
    }

    [CustomEditor(typeof(WaterBody), true), CanEditMultipleObjects]
    class WaterBodyEditor : ValidatedEditor { }
#endif
}
