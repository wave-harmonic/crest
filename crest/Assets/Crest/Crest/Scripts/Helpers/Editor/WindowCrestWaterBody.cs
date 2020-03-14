// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    public class WindowCrestWaterBody : EditorWindow
    {
        // This is required because gizmos don't intersect with scene, which makes them useless as a guide when placing
        GameObject _proxyObject;
        bool _showProxy = true;

        // Placement
        Vector3 _position = Vector3.zero;
        float _sizeX = 100f;
        float _sizeZ = 100f;
        float _rotation = 0f;

        bool _createDepthCache = true;
        string _depthCacheLayerName = "Default";

        bool _createGerstnerWaves = false;
        float _gerstnerWindDirection = 0f;
        OceanWaveSpectrum _gerstnerWaveSpectrum = null;
        Material _gerstnerMaterial = null;

        bool _createClipArea = false;
        Material _clipMaterial = null;

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
            _showProxy = EditorGUILayout.Toggle("Show layout proxy", _showProxy);
            _position = EditorGUILayout.Vector3Field("Center position", _position);
            _sizeX = EditorGUILayout.FloatField("Size X", _sizeX);
            _sizeZ = EditorGUILayout.FloatField("Size Z", _sizeZ);
            _rotation = EditorGUILayout.FloatField("Rotation", _rotation);

            EditorGUILayout.Space();

            _createDepthCache = EditorGUILayout.BeginToggleGroup("Create Depth Cache", _createDepthCache);
            _depthCacheLayerName = EditorGUILayout.TextField("Layer for cache", _depthCacheLayerName);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();

            _createGerstnerWaves = EditorGUILayout.BeginToggleGroup("Create Gerstner Waves", _createGerstnerWaves);
            _gerstnerWindDirection = EditorGUILayout.FloatField("Wind direction angle", _gerstnerWindDirection);
            _gerstnerWaveSpectrum = EditorGUILayout.ObjectField("Wave spectrum", _gerstnerWaveSpectrum, typeof(OceanWaveSpectrum), false) as OceanWaveSpectrum;
            _gerstnerMaterial = EditorGUILayout.ObjectField("Gerstner material", _gerstnerMaterial, typeof(Material), false) as Material;
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();

            _createClipArea = EditorGUILayout.BeginToggleGroup("Create Clip Area", _createClipArea);
            _clipMaterial = EditorGUILayout.ObjectField("Clip material", _clipMaterial, typeof(Material), false) as Material;
            EditorGUILayout.EndToggleGroup();

            if (EditorGUI.EndChangeCheck())
            {
                UpdateProxy();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create"))
            {
                CreateWaterBody();
            }
        }

        [MenuItem("Window/Crest/Create Water Body")]
        public static void ShowWindow()
        {
            GetWindow<WindowCrestWaterBody>("Crest Create Water Body");
        }

        private void OnFocus()
        {
            // Remove delegate listener if it has previously
            // been assigned.
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            // Add (or re-add) the delegate.
            SceneView.onSceneGUIDelegate += OnSceneGUI;

            if (_proxyObject == null)
            {
                CreateProxyObject();
            }
        }

        private void OnDestroy()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;

            if (_proxyObject != null)
            {
                DestroyImmediate(_proxyObject);
            }
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (!_showProxy)
            {
                return;
            }

            _position = Handles.DoPositionHandle(_position, Quaternion.identity);
            UpdateProxy();
        }

        void CreateProxyObject()
        {
            _proxyObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _proxyObject.name = "_HIDDEN_WaterBodyProxy";
            _proxyObject.hideFlags = HideFlags.HideAndDontSave;
            UpdateProxy();
        }

        void UpdateProxy()
        {
            _proxyObject.transform.position = _position;
            _proxyObject.transform.rotation = Quaternion.AngleAxis(_rotation, Vector3.up);
            _proxyObject.transform.localScale = new Vector3(_sizeX / 10f, 1f, _sizeZ / 10f);
            _proxyObject.SetActive(_showProxy);
        }

        void CreateWaterBody()
        {
            var waterBodyGO = new GameObject("WaterBody");
            waterBodyGO.transform.position = _position;
            waterBodyGO.transform.rotation = Quaternion.AngleAxis(_rotation, Vector3.up);
            waterBodyGO.transform.localScale = new Vector3(_sizeX, 1f, _sizeZ);

            waterBodyGO.AddComponent<WaterBody>();

            if (_createDepthCache)
            {
                var depthCacheGO = new GameObject("DepthCache");
                depthCacheGO.transform.parent = waterBodyGO.transform;
                depthCacheGO.transform.localRotation = Quaternion.identity;
                depthCacheGO.transform.localPosition = Vector3.zero;
                depthCacheGO.transform.localScale = Vector3.one;

                var depthCache = depthCacheGO.AddComponent<OceanDepthCache>();
                var res = Mathf.FloorToInt(Mathf.Max(_sizeX, _sizeZ) / 0.5f);
                // I think multiple-of-4 is typical requirement for texture compression
                if (res % 4 > 0) res += 4 - (res % 4);
                depthCache._resolution = Mathf.Clamp(res, 16, 512);
                depthCache._layerNames = new string[] { _depthCacheLayerName };
            }

            if (_createGerstnerWaves)
            {
                var gerstnerGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                gerstnerGO.name = "GerstnerWaves";
                DestroyImmediate(gerstnerGO.GetComponent<Collider>());
                gerstnerGO.transform.parent = waterBodyGO.transform;
                gerstnerGO.transform.localEulerAngles = 90f * Vector3.right;
                gerstnerGO.transform.localScale = Vector3.one;
                gerstnerGO.transform.localPosition = Vector3.zero;

                var gerstner = gerstnerGO.AddComponent<ShapeGerstnerBatched>();
                gerstner._mode = ShapeGerstnerBatched.GerstnerMode.Geometry;
                gerstner._windDirectionAngle = _gerstnerWindDirection;
                gerstner._spectrum = _gerstnerWaveSpectrum;

                var rend = gerstnerGO.GetComponent<Renderer>();
                rend.sharedMaterial = _gerstnerMaterial;
            }

            if (_createClipArea)
            {
                var clipGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                clipGO.name = "SurfaceClip";
                DestroyImmediate(clipGO.GetComponent<Collider>());
                clipGO.transform.parent = waterBodyGO.transform;
                clipGO.transform.localEulerAngles = 90f * Vector3.right;
                clipGO.transform.localScale = Vector3.one;
                clipGO.transform.localPosition = Vector3.zero;

                clipGO.AddComponent<RegisterClipSurfaceInput>();

                var rend = clipGO.GetComponent<Renderer>();
                rend.sharedMaterial = _clipMaterial;
            }
        }
    }
}
