// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Crest
{
    public class WindowCrestWaterBody : EditorWindow
    {
        enum State
        {
            Idle,
            Placing
        }
        State _state;

        // This is required because gizmos don't intersect with scene, which makes them useless as a guide when placing
        GameObject _proxyObject;

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
            switch (_state)
            {
                case State.Idle:
                    OnGUIIdle();
                    break;
                case State.Placing:
                    OnGUIPlacing();
                    break;
            }
        }

        private void OnGUIIdle()
        {
            if (GUILayout.Button("Create Water Body"))
            {
                _state = State.Placing;

                // Refresh scene view
                GetWindow<SceneView>();
            }
        }
        Vector2 _scrollPosition;

        private void OnGUIPlacing()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
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
            if (_createClipArea)
            {
                EditorGUILayout.HelpBox("Create Clip Surface Data should be enabled on the OceanRnederer component, and the Default Clipping State should be set to Everything Clipped.", MessageType.Info);
            }
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

            if (GUILayout.Button("Done"))
            {
                _state = State.Idle;

                // Refresh scene view
                GetWindow<SceneView>();
            }

            EditorGUILayout.EndScrollView();
        }

        [MenuItem("Window/Crest/Create Water Body")]
        public static void ShowWindow()
        {
            GetWindow<WindowCrestWaterBody>("Crest Create Water Body");
        }

        private void OnEnable()
        {
            PopulateResources();

            var ocean = FindObjectOfType<OceanRenderer>();
            _position.y = (ocean != null && ocean.Root != null) ? ocean.Root.position.y : 0f;
        }

        private void OnFocus()
        {
            // Remove delegate listener if it has previously
            // been assigned.
            SceneView.duringSceneGui -= OnSceneGUI;
            // Add (or re-add) the delegate.
            SceneView.duringSceneGui += OnSceneGUI;

            if (_proxyObject == null)
            {
                CreateProxyObject();
            }
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;

            if (_proxyObject != null)
            {
                DestroyImmediate(_proxyObject);
            }
        }

        void OnSceneGUI(SceneView sceneView)
        {
            UpdateProxy();

            if (_state != State.Placing)
            {
                return;
            }

            _position = Handles.DoPositionHandle(_position, Quaternion.identity);
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
            var planeScaleFactor = 10f;
            _proxyObject.transform.localScale = new Vector3(_sizeX / planeScaleFactor, 1f, _sizeZ / planeScaleFactor);
            _proxyObject.SetActive(_state == State.Placing);
        }

        bool PopulateResources()
        {
            if (_clipMaterial == null)
            {
                _clipMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Crest/Crest/Materials/OceanInputs/ClipSurfaceIncludeArea.mat");
                if (_clipMaterial == null) return false;
            }

            if (_gerstnerMaterial == null)
            {
                _gerstnerMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Crest/Crest/Materials/OceanInputs/WaterBodyGerstnerPatch.mat");
                if (_gerstnerMaterial == null) return false;
            }

            return true;
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

        bool CheckResources()
        {
            if (_createClipArea && _clipMaterial == null)
            {
                Debug.LogError("A material for the clip shader must be provided. This is typically a material using shader 'Crest/Inputs/Clip Surface/Include Area'");
                return false;
            }

            if (_createGerstnerWaves && _gerstnerMaterial == null)
            {
                Debug.LogError("A material for the Gerstner waves must be specified in the Create Water Body window. This is typically a material using shader 'Crest/Inputs/Animated Waves/Gerstner Batch Geometry'");
                return false;
            }

            return true;
        }
    }
}

#endif
