// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// This script originated from the unity standard assets. It has been modified heavily to be camera-centric (as opposed to
// geometry-centric) and assumes a single main camera which simplifies the code.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Crest
{
    internal static class PreparedReflections
    {
        private static volatile RenderTexture _currentreflectiontexture = null;
        private static volatile int _referenceCameraInstanceId = -1;
        private static volatile KeyValuePair<int, RenderTexture>[] _collection = new KeyValuePair<int, RenderTexture>[0];

        public static RenderTexture GetRenderTexture(int camerainstanceid)
        {
            if (camerainstanceid == _referenceCameraInstanceId)
                return _currentreflectiontexture;

            // Prevent crash if somebody change collection now in over thread, useless in unity now
            var currentcollection = _collection;
            for (int i = 0; i < currentcollection.Length; i++)
            {
                if (currentcollection[i].Key == camerainstanceid)
                {
                    var texture = currentcollection[i].Value;
                    _currentreflectiontexture = texture;
                    _referenceCameraInstanceId = camerainstanceid;
                    return texture;
                }
            }
            return null;
        }

        // Remove element if exists
        public static void Remove(int camerainstanceid)
        {
            if (!GetRenderTexture(camerainstanceid)) return;
            _collection = _collection.Where(e => e.Key != camerainstanceid).ToArray(); //rebuild array without element
            _currentreflectiontexture = null;
            _referenceCameraInstanceId = -1;
        }

        public static void Register(int instanceId, RenderTexture reflectionTexture)
        {
            var currentcollection = _collection;
            for (var i = 0; i < currentcollection.Length; i++)
            {
                if (currentcollection[i].Key == instanceId)
                {
                    currentcollection[i] = new KeyValuePair<int, RenderTexture>(instanceId, reflectionTexture);
                    return;
                }
            }
            // Rebuild with new element if not found
            _collection = currentcollection
                .Append(new KeyValuePair<int, RenderTexture>(instanceId, reflectionTexture)).ToArray();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            _currentreflectiontexture = null;
            _referenceCameraInstanceId = -1;
            _collection = new KeyValuePair<int, RenderTexture>[0];
        }
    }

    /// <summary>
    /// Attach to a camera to generate a reflection texture which can be sampled in the ocean shader.
    /// </summary>
    [ExecuteDuringEditMode]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Ocean Planar Reflections")]
    public partial class OceanPlanarReflection : CustomMonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [SerializeField] LayerMask _reflectionLayers = 1;
        [SerializeField] bool _disableOcclusionCulling = true;
        [SerializeField] bool _disablePixelLights = true;
        [SerializeField] bool _disableShadows = false;
        [SerializeField] int _textureSize = 256;
        [SerializeField] float _clipPlaneOffset = 0.07f;
        [SerializeField] bool _hdr = true;
        [SerializeField] bool _stencil = false;
        [SerializeField] bool _hideCameraGameobject = true;
        [SerializeField] bool _allowMSAA = false;           //allow MSAA on reflection camera
        [SerializeField] float _farClipPlane = 1000;             //far clip plane for reflection camera on all layers
        [SerializeField] bool _forceForwardRenderingPath = true;

        [Tooltip("The Color option will skip skybox rendering and fallback to global reflections (minor optimization), but any alpha shaders that do not write alpha will not appear in planar reflections (eg tree leaves). Use Skybox for best compatibility.")]
        [SerializeField] CameraClearFlags _clearFlags = CameraClearFlags.Skybox;

        /// <summary>
        /// Refresh reflection every x frames(1-every frame)
        /// </summary>
        [SerializeField] int RefreshPerFrames = 1;

        /// <summary>
        /// To relax OceanPlanarReflection refresh to different frames need to set different values for each script
        /// </summary>
        [SerializeField] int _frameRefreshOffset = 0;

        RenderTexture _reflectionTexture;

        Camera _camViewpoint;
        Skybox _camViewpointSkybox;
        Camera _camReflections;
        public Camera ReflectionCamera => _camReflections;
        Skybox _camReflectionsSkybox;

        private long _lastRefreshOnFrame = -1;

        const int CULL_DISTANCE_COUNT = 32;
        float[] _cullDistances = new float[CULL_DISTANCE_COUNT];

#if UNITY_EDITOR
        bool _isSceneCamera;
        float _changeViewTimer;
#endif

        private void Start()
        {
            if (!TryGetComponent(out _camViewpoint))
            {
                Debug.LogWarning("Crest: Disabling planar reflections as no camera found on gameobject to generate reflection from.", this);
                enabled = false;
                return;
            }
            _camViewpointSkybox = _camViewpoint.GetComponent<Skybox>();

            // This is anyway called in OnPreRender, but was required here as there was a black reflection
            // for a frame without this earlier setup call.
            CreateWaterObjects(_camViewpoint);

#if UNITY_EDITOR
            if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_PLANARREFLECTIONS_ON"))
            {
                Debug.LogWarning("Crest: Planar reflections are not enabled on the current ocean material and will not be visible.", this);
            }
#endif

#if UNITY_EDITOR
            // Otherwise will not work without entering/exiting play mode.
            if (_camViewpoint.CompareTag("MainCamera"))
            {
                enabled = false;
                enabled = true;
            }
#endif
        }

        void LateUpdate()
        {
            if (!RequestRefresh(Time.renderedFrameCount))
                return; // Skip if not need to refresh on this frame

            if (OceanRenderer.Instance == null)
            {
                return;
            }

#if UNITY_EDITOR
            // Work in edit mode.
            var editorCamera = OceanRenderer.Instance.ViewCamera;
            if (_camViewpoint.CompareTag("MainCamera") && editorCamera != null && editorCamera.cameraType == CameraType.SceneView)
            {
                if (!editorCamera.TryGetComponent<OceanPlanarReflection>(out var editor))
                {
                    editor = editorCamera.gameObject.AddComponent<OceanPlanarReflection>();
                    editor._isSceneCamera = true;
                }

                if (editor != null)
                {
                    editor._reflectionLayers = _reflectionLayers;
                    editor._disableOcclusionCulling = _disableOcclusionCulling;
                    editor._disablePixelLights = _disablePixelLights;
                    editor._disableShadows = _disableShadows;
                    editor._textureSize = _textureSize;
                    editor._clipPlaneOffset = _clipPlaneOffset;
                    editor._hdr = _hdr;
                    editor._stencil = _stencil;
                    editor._hideCameraGameobject = _hideCameraGameobject;
                    editor._allowMSAA = _allowMSAA;
                    editor._farClipPlane = _farClipPlane;
                    editor._forceForwardRenderingPath = _forceForwardRenderingPath;
                    editor._clearFlags = _clearFlags;
                }
            }
#endif

            CreateWaterObjects(_camViewpoint);

            if (!_camReflections)
            {
                return;
            }

#if UNITY_EDITOR
            // Only run one camera at a time.
            if (_isSceneCamera)
            {
                if (Application.isPlaying)
                {
                    return;
                }

                // Fix "Screen position out of view frustum" when 2D view activated.
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView != null && sceneView.in2DMode)
                {
                    return;
                }
            }
            else if (!Application.isPlaying)
            {
                return;
            }
#endif

            // Find out the reflection plane: position and normal in world space
            Vector3 planePos = OceanRenderer.Instance.Root.position;
            Vector3 planeNormal = Vector3.up;

            UpdateCameraModes();

            var offset = _clipPlaneOffset;
            {
                var viewpoint = _camViewpoint.transform;
                if (offset == 0f && viewpoint.position.y == planePos.y)
                {
                    // Minor offset to prevent "Screen position out of view frustum". Could be BIRP only.
                    offset = -0.00001f;
                }
            }

            // Reflect camera around reflection plane
            float d = -Vector3.Dot(planeNormal, planePos) - offset;
            Vector4 reflectionPlane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, d);

            Matrix4x4 reflection = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflection, reflectionPlane);
            Vector3 newpos = reflection.MultiplyPoint(_camViewpoint.transform.position);
            _camReflections.worldToCameraMatrix = _camViewpoint.worldToCameraMatrix * reflection;

            // Setup oblique projection matrix so that near plane is our reflection
            // plane. This way we clip everything below/above it for free.
            Vector4 clipPlane = CameraSpacePlane(_camReflections, planePos, planeNormal, 1.0f, offset);
            _camReflections.projectionMatrix = _camViewpoint.CalculateObliqueMatrix(clipPlane);

            // Set custom culling matrix from the current camera
            _camReflections.cullingMatrix = _camViewpoint.projectionMatrix * _camViewpoint.worldToCameraMatrix;

            _camReflections.targetTexture = _reflectionTexture;

            _camReflections.transform.position = newpos;
            Vector3 euler = _camViewpoint.transform.eulerAngles;
            _camReflections.transform.eulerAngles = new Vector3(-euler.x, euler.y, euler.z);
            _camReflections.cullingMatrix = _camReflections.projectionMatrix * _camReflections.worldToCameraMatrix;

            ForceDistanceCulling(_farClipPlane);

            // We do not want the water plane when rendering planar reflections.
            OceanRenderer.Instance.Root.gameObject.SetActive(false);

            // Invert culling because view is mirrored
            bool oldCulling = GL.invertCulling;
            GL.invertCulling = !oldCulling;

            // Optionally disable pixel lights for reflection/refraction
            int oldPixelLightCount = QualitySettings.pixelLightCount;
            if (_disablePixelLights) QualitySettings.pixelLightCount = 0;

            // Optionally disable shadows for reflection/refraction
            var oldShadowQuality = QualitySettings.shadows;
            if (_disableShadows) QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;

            try
            {
                _camReflections.Render();
            }
            finally
            {
                // Restore global settings.
                GL.invertCulling = oldCulling;
                if (_disableShadows) QualitySettings.shadows = oldShadowQuality;
                if (_disablePixelLights) QualitySettings.pixelLightCount = oldPixelLightCount;
            }

            OceanRenderer.Instance.Root.gameObject.SetActive(true);

            // Remember this frame as last refreshed.
            Refreshed(Time.renderedFrameCount);
        }

        bool RequestRefresh(long frame)
        {
            if (_lastRefreshOnFrame <= 0 || RefreshPerFrames < 2)
            {
                // Not refreshed before or refresh every frame, not check frame counter.
                return true;
            }

            return Math.Abs(_frameRefreshOffset) % RefreshPerFrames == frame % RefreshPerFrames;
        }

        void Refreshed(long currentframe)
        {
            _lastRefreshOnFrame = currentframe;
        }

        /// <summary>
        /// Limit render distance for reflection camera for first 32 layers
        /// </summary>
        /// <param name="farClipPlane">reflection far clip distance</param>
        private void ForceDistanceCulling(float farClipPlane)
        {
            if (_cullDistances == null || _cullDistances.Length != CULL_DISTANCE_COUNT)
                _cullDistances = new float[CULL_DISTANCE_COUNT];
            for (var i = 0; i < _cullDistances.Length; i++)
            {
                // The culling distance
                _cullDistances[i] = farClipPlane;
            }
            _camReflections.layerCullDistances = _cullDistances;
            _camReflections.layerCullSpherical = true;
        }

        void UpdateCameraModes()
        {
            _camReflections.cullingMask = _reflectionLayers;

            // Set water camera to clear the same way as current camera
            _camReflections.renderingPath = _forceForwardRenderingPath ? RenderingPath.Forward : _camViewpoint.renderingPath;
            _camReflections.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camReflections.clearFlags = _clearFlags;

            if (_clearFlags == CameraClearFlags.Skybox)
            {
                if (!_camViewpointSkybox || !_camViewpointSkybox.material)
                {
                    _camReflectionsSkybox.enabled = false;
                }
                else
                {
                    _camReflectionsSkybox.enabled = true;
                    _camReflectionsSkybox.material = _camViewpointSkybox.material;
                }
            }

            // Update other values to match current camera.
            // Even if we are supplying custom camera&projection matrices,
            // some of values are used elsewhere (e.g. skybox uses far plane).

            _camReflections.farClipPlane = _camViewpoint.farClipPlane;
            _camReflections.nearClipPlane = _camViewpoint.nearClipPlane;
            _camReflections.orthographic = _camViewpoint.orthographic;
            _camReflections.fieldOfView = _camViewpoint.fieldOfView;
            _camReflections.orthographicSize = _camViewpoint.orthographicSize;
            _camReflections.allowMSAA = _allowMSAA;
            _camReflections.aspect = _camViewpoint.aspect;
            _camReflections.useOcclusionCulling = !_disableOcclusionCulling && _camViewpoint.useOcclusionCulling;
        }

        // On-demand create any objects we need for water
        void CreateWaterObjects(Camera currentCamera)
        {
            // Reflection render texture
            if (!_reflectionTexture || _reflectionTexture.width != _textureSize)
            {
                if (_reflectionTexture)
                {
                    Helpers.Destroy(_reflectionTexture);
                }

                var format = _hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
                Debug.Assert(SystemInfo.SupportsRenderTextureFormat(format), "Crest: The graphics device does not support the render texture format " + format.ToString());
                _reflectionTexture = new RenderTexture(_textureSize, _textureSize, _stencil ? 24 : 16, format)
                {
                    name = "__WaterReflection" + GetHashCode(),
                    isPowerOfTwo = true,
                };
                _reflectionTexture.Create();
                PreparedReflections.Register(currentCamera.GetHashCode(), _reflectionTexture);
            }

            // Camera for reflection
            if (!_camReflections)
            {
                _camReflections = new GameObject("Crest Water Reflection Camera").AddComponent<Camera>();
#if UNITY_EDITOR
                _camReflections.name = $"Crest Water Reflection Camera ({currentCamera.name})";
#endif
                _camReflections.enabled = false;
                _camReflections.transform.SetPositionAndRotation(transform.position, transform.rotation);
                _camReflectionsSkybox = _camReflections.gameObject.AddComponent<Skybox>();
                _camReflections.gameObject.AddComponent<FlareLayer>();
                _camReflections.cameraType = CameraType.Reflection;
            }

            _camReflections.gameObject.hideFlags = _hideCameraGameobject ? HideFlags.HideAndDontSave : HideFlags.DontSave;
        }

        // Given position/normal of the plane, calculates plane in camera space.
        Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign, float offset)
        {
            Vector3 offsetPos = pos + normal * offset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        // Calculates reflection matrix around the given plane
        static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }

        private void OnDisable()
        {
            if (_camViewpoint != null)
            {
                PreparedReflections.Remove(_camViewpoint.GetHashCode());
            }

            if (_camReflections)
            {
                _camReflections.targetTexture = null;
                Helpers.Destroy(_camReflections.gameObject);
                _camReflections = null;
            }

            // Cleanup all the objects we possibly have created
            if (_reflectionTexture)
            {
                Helpers.Destroy(_reflectionTexture);
                _reflectionTexture = null;
            }
        }
    }

#if UNITY_EDITOR
    public partial class OceanPlanarReflection : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            if (_clearFlags != CameraClearFlags.Skybox)
            {
                showMessage
                (
                    "The <i>Clear Flags</i> is not set to <i>Skybox</i>. " +
                    "Any shaders which do not write alpha (eg some tree leaves) will not appear in the final reflections.",
                    "Change <i>Clear Flags</i> to <i>Skybox</i>.",
                    ValidatedHelper.MessageType.Info, this,
                    (x) => x.FindProperty(nameof(_clearFlags)).intValue = (int)CameraClearFlags.Skybox
                );
            }

            return isValid;
        }
    }
#endif
}
