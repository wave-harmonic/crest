// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.SceneManagement;

namespace Crest
{
    [ExecuteAlways]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_DEBUG + "Ocean Debug GUI")]
    public class OceanDebugGUI : MonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public bool _showOceanData = true;
        public bool _guiVisible = true;

        [SerializeField] bool _drawLodDatasActualSize = false;

        [Header("Lod Datas")]
        [SerializeField] bool _drawAnimWaves = true;
        [SerializeField] bool _drawDynWaves = false;
        [SerializeField] bool _drawFoam = false;
        [SerializeField] bool _drawFlow = false;
        [SerializeField] bool _drawShadow = false;
        [SerializeField] bool _drawSeaFloorDepth = false;
        [SerializeField] bool _drawClipSurface = false;

        readonly static float _leftPanelWidth = 180f;
        readonly static float _bottomPanelHeight = 25f;
        readonly static Color _guiColor = Color.black * 0.7f;

        static readonly Dictionary<System.Type, string> s_simNames = new Dictionary<System.Type, string>();

        static Dictionary<RenderTexture, Material> s_textureArrayMaterials = new Dictionary<RenderTexture, Material>();

        static OceanDebugGUI s_Instance;

        public static bool OverGUI(Vector2 screenPosition)
        {
            return s_Instance != null && (s_Instance._guiVisible && screenPosition.x < _leftPanelWidth ||
                s_Instance._showOceanData && screenPosition.y < _bottomPanelHeight);
        }

        void OnEnable()
        {
            s_Instance = this;
        }

        void OnDisable()
        {
            s_Instance = null;
        }

        private void Update()
        {
            // New input system works even when game view is not focused.
            if (!Application.isFocused)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.gKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.G))
#endif
            {
                ToggleGUI();
            }
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.fKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.F))
#endif
            {
                Time.timeScale = Time.timeScale == 0f ? 1f : 0f;
            }
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.rKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.R))
#endif
            {
                SceneManager.LoadScene(SceneManager.GetSceneAt(0).buildIndex);
            }
        }

        void OnGUI()
        {
            Color bkp = GUI.color;

            if (_guiVisible)
            {
                GUI.skin.toggle.normal.textColor = Color.white;
                GUI.skin.label.normal.textColor = Color.white;

                float x = 5f, y = 0f;
                float w = _leftPanelWidth - 2f * x, h = 25f;

                GUI.color = _guiColor;
                GUI.DrawTexture(new Rect(0, 0, w + 2f * x, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;

                RenderWireFrame._wireFrame = GUI.Toggle(new Rect(x, y, w, h), RenderWireFrame._wireFrame, "Wireframe"); y += h;

                GUI.changed = false;
                bool freeze = GUI.Toggle(new Rect(x, y, w, h), Time.timeScale == 0f, "Freeze time (F)"); y += h;
                if (GUI.changed)
                {
                    Time.timeScale = freeze ? 0f : 1f;
                }

                // Global wind speed
                if (OceanRenderer.Instance)
                {
                    GUI.Label(new Rect(x, y, w, h), "Global Wind Speed"); y += h;
                    OceanRenderer.Instance._globalWindSpeed = GUI.HorizontalSlider(new Rect(x, y, w, h), OceanRenderer.Instance._globalWindSpeed, 0f, 150f); y += h;
                }

                OnGUIGerstnerSection(x, ref y, w, h);

                _showOceanData = GUI.Toggle(new Rect(x, y, w, h), _showOceanData, "Show sim data"); y += h;

                LodDataMgrAnimWaves._shapeCombinePass = GUI.Toggle(new Rect(x, y, w, h), LodDataMgrAnimWaves._shapeCombinePass, "Shape combine pass"); y += h;

                LodDataMgrShadow.s_processData = GUI.Toggle(new Rect(x, y, w, h), LodDataMgrShadow.s_processData, "Process Shadows"); y += h;

                if (OceanRenderer.Instance)
                {
                    if (OceanRenderer.Instance._lodDataDynWaves != null)
                    {
                        var dt = 1f / OceanRenderer.Instance._lodDataDynWaves.Settings._simulationFrequency;
                        var steps = OceanRenderer.Instance._lodDataDynWaves.LastUpdateSubstepCount;
                        GUI.Label(new Rect(x, y, w, h), string.Format("Sim steps: {0:0.00000} x {1}", dt, steps)); y += h;
                    }

                    var querySystem = OceanRenderer.Instance.CollisionProvider as QueryBase;
                    if (OceanRenderer.Instance.CollisionProvider != null && querySystem != null)
                    {
                        GUI.Label(new Rect(x, y, w, h), $"Query result GUIDs: {querySystem.ResultGuidCount}"); y += h;
                        GUI.Label(new Rect(x, y, w, h), $"Queries in flight: {querySystem.RequestCount}"); y += h;
                    }

#if UNITY_EDITOR
                    if (GUI.Button(new Rect(x, y, w, h), "Select Ocean Mat"))
                    {
                        var path = UnityEditor.AssetDatabase.GetAssetPath(OceanRenderer.Instance.OceanMaterial);
                        var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
                        UnityEditor.Selection.activeObject = asset;
                    }
                    y += h;
#endif
                }

                if (GUI.Button(new Rect(x, y, w, h), "Hide GUI (G)"))
                {
                    ToggleGUI();
                }
                y += h;
            }

            // draw source textures to screen
            if (_showOceanData)
            {
                DrawShapeTargets();
            }

            GUI.color = bkp;
        }

        void OnGUIGerstnerSection(float x, ref float y, float w, float h)
        {
            GUI.Label(new Rect(x, y, w, h), "Gerstner weight(s)"); y += h;

            foreach (var gerstner in ShapeGerstnerBatched.Instances)
            {
                var specW = 75f;
                gerstner.Value._weight = GUI.HorizontalSlider(new Rect(x, y, w - specW - 5f, h), gerstner.Value._weight, 0f, 1f);

#if UNITY_EDITOR
                if (GUI.Button(new Rect(x + w - specW, y, specW, h), "Spectrum"))
                {
                    var path = UnityEditor.AssetDatabase.GetAssetPath(gerstner.Value._spectrum);
                    var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
                    UnityEditor.Selection.activeObject = asset;
                }
#endif
                y += h;
            }

            foreach (var gerstner in ShapeGerstner.Instances)
            {
                var specW = 75f;
                gerstner.Value._weight = GUI.HorizontalSlider(new Rect(x, y, w - specW - 5f, h), gerstner.Value._weight, 0f, 1f);

#if UNITY_EDITOR
                if (GUI.Button(new Rect(x + w - specW, y, specW, h), "Spectrum"))
                {
                    var path = UnityEditor.AssetDatabase.GetAssetPath(gerstner.Value._spectrum);
                    var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
                    UnityEditor.Selection.activeObject = asset;
                }
#endif
                y += h;
            }

            GUI.Label(new Rect(x, y, w, h), $"FFT generator(s): {FFTCompute.GeneratorCount}"); y += h;
        }

        void DrawShapeTargets()
        {
            if (OceanRenderer.Instance == null) return;

            // Draw bottom panel for toggles
            var bottomBar = new Rect(_guiVisible ? _leftPanelWidth : 0,
                Screen.height - _bottomPanelHeight, Screen.width, _bottomPanelHeight);
            GUI.color = _guiColor;
            GUI.DrawTexture(bottomBar, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Show viewer height above water in bottom panel
            bottomBar.x += 10;
            GUI.Label(bottomBar, "Viewer Height Above Water: " + OceanRenderer.Instance.ViewerHeightAboveWater);

            // Draw sim data
            DrawSims();
        }

        void DrawSims()
        {
            float column = 1f;

            DrawSim<LodDataMgrAnimWaves>(OceanRenderer.Instance._lodDataAnimWaves, _drawLodDatasActualSize, ref _drawAnimWaves, ref column, 0.5f);
            DrawSim<LodDataMgrDynWaves>(OceanRenderer.Instance._lodDataDynWaves, _drawLodDatasActualSize, ref _drawDynWaves, ref column, 0.5f, 2f);
            DrawSim<LodDataMgrFoam>(OceanRenderer.Instance._lodDataFoam, _drawLodDatasActualSize, ref _drawFoam, ref column);
            DrawSim<LodDataMgrFlow>(OceanRenderer.Instance._lodDataFlow, _drawLodDatasActualSize, ref _drawFlow, ref column, 0.5f, 2f);
            DrawSim<LodDataMgrShadow>(OceanRenderer.Instance._lodDataShadow, _drawLodDatasActualSize, ref _drawShadow, ref column);
            DrawSim<LodDataMgrSeaFloorDepth>(OceanRenderer.Instance._lodDataSeaDepths, _drawLodDatasActualSize, ref _drawSeaFloorDepth, ref column);
            DrawSim<LodDataMgrClipSurface>(OceanRenderer.Instance._lodDataClipSurface, _drawLodDatasActualSize, ref _drawClipSurface, ref column);
        }

        static void DrawSim<SimType>(LodDataMgr lodData, bool actualSize, ref bool doDraw, ref float offset, float bias = 0f, float scale = 1f) where SimType : LodDataMgr
        {
            if (lodData == null) return;

            var type = typeof(SimType);
            if (!s_simNames.ContainsKey(type))
            {
                s_simNames.Add(type, type.Name.Substring(10));
            }

            float togglesBegin = Screen.height - _bottomPanelHeight;
            float b = 7f;
            float h = actualSize ? lodData.DataTexture.height : togglesBegin / (float)lodData.DataTexture.volumeDepth;
            float w = h + b;
            float x = Screen.width - w * offset + b * (offset - 1f);

            if (doDraw)
            {
                // Background behind slices
                GUI.color = _guiColor;
                GUI.DrawTexture(new Rect(x, 0, offset == 1f ? w : w - b, Screen.height - _bottomPanelHeight), Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Only use Graphics.DrawTexture in EventType.Repaint events if called in OnGUI
                if (Event.current.type.Equals(EventType.Repaint))
                {
                    for (int idx = 0; idx < lodData.DataTexture.volumeDepth; idx++)
                    {
                        float y = idx * h;
                        if (offset == 1f) w += b;

                        s_textureArrayMaterials.TryGetValue(lodData.DataTexture, out var material);
                        if (material == null)
                        {
                            material = new Material(Shader.Find("Hidden/Crest/Debug/TextureArray"));
                            s_textureArrayMaterials.Add(lodData.DataTexture, material);
                        }

                        // Render specific slice of 2D texture array
                        material.SetInt("_Depth", idx);
                        material.SetFloat("_Scale", scale);
                        material.SetFloat("_Bias", bias);
                        Graphics.DrawTexture(new Rect(x + b, y + b / 2f, h - b, h - b), lodData.DataTexture, material);
                    }
                }
            }

            doDraw = GUI.Toggle(new Rect(x + b, togglesBegin, w - 2f * b, _bottomPanelHeight), doDraw, s_simNames[type]);

            offset++;
        }

        public static void DrawTextureArray(RenderTexture data, int columnOffsetFromRightSide, float bias = 0f, float scale = 1f)
        {
            int offset = columnOffsetFromRightSide;

            float togglesBegin = Screen.height - _bottomPanelHeight;
            float b = 1f;
            float h = togglesBegin / (float)data.volumeDepth;
            float w = h + b;
            float x = Screen.width - w * offset + b * (offset - 1f);

            {
                // Background behind slices
                GUI.color = _guiColor;
                GUI.DrawTexture(new Rect(x, 0, offset == 1f ? w : w - b, Screen.height - _bottomPanelHeight), Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Only use Graphics.DrawTexture in EventType.Repaint events if called in OnGUI
                if (Event.current.type.Equals(EventType.Repaint))
                {
                    for (int idx = 0; idx < data.volumeDepth; idx++)
                    {
                        float y = idx * h;
                        if (offset == 1f) w += b;

                        s_textureArrayMaterials.TryGetValue(data, out var material);
                        if (material == null)
                        {
                            material = new Material(Shader.Find("Hidden/Crest/Debug/TextureArray"));
                            s_textureArrayMaterials.Add(data, material);
                        }

                        // Render specific slice of 2D texture array
                        material.SetInt("_Depth", idx);
                        material.SetFloat("_Scale", scale);
                        material.SetFloat("_Bias", bias);
                        Graphics.DrawTexture(new Rect(x + b, y + b / 2f, h - b, h - b), data, material);
                    }
                }
            }
        }

        void ToggleGUI()
        {
            _guiVisible = !_guiVisible;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_simNames.Clear();
            s_textureArrayMaterials.Clear();
        }
    }
}
