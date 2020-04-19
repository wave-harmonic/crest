// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crest
{
    public class OceanDebugGUI : MonoBehaviour
    {
        public bool _showOceanData = true;
        public bool _guiVisible = true;
        readonly static float _leftPanelWidth = 180f;
        readonly static float _bottomPanelHeight = 25f;
        readonly static Color _guiColor = Color.black * 0.7f;
        ShapeGerstnerBatched[] _gerstners;

        static readonly Dictionary<System.Type, bool> s_drawTargets = new Dictionary<System.Type, bool>();
        static readonly Dictionary<System.Type, string> s_simNames = new Dictionary<System.Type, string>();

        static Material s_textureArrayMaterial = null;

        void Awake()
        {
            if (s_textureArrayMaterial == null)
            {
                s_textureArrayMaterial = new Material(Shader.Find("Hidden/Crest/Debug/TextureArray"));
            }
        }

        public static bool OverGUI(Vector2 screenPosition)
        {
            return screenPosition.x < _leftPanelWidth;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                ToggleGUI();
            }
            if (Input.GetKeyDown(KeyCode.F))
            {
                Time.timeScale = Time.timeScale == 0f ? 1f : 0f;
            }
            if (Input.GetKeyDown(KeyCode.R))
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

                GUI.Label(new Rect(x, y, w, h), "Gerstner weight(s)"); y += h;
                if (_gerstners == null)
                {
                    _gerstners = FindObjectsOfType<ShapeGerstnerBatched>();
                    // i am getting the array in the reverse order compared to the hierarchy which bugs me. sort them based on sibling index,
                    // which helps if the Gerstners are on sibling GOs.
                    System.Array.Sort(_gerstners, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
                }
                foreach (var gerstner in _gerstners)
                {
                    var specW = 75f;
                    gerstner._weight = GUI.HorizontalSlider(new Rect(x, y, w - specW - 5f, h), gerstner._weight, 0f, 1f);

#if UNITY_EDITOR
                    if (GUI.Button(new Rect(x + w - specW, y, specW, h), "Spectrum"))
                    {
                        var path = UnityEditor.AssetDatabase.GetAssetPath(gerstner._spectrum);
                        var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
                        UnityEditor.Selection.activeObject = asset;
                    }
#endif
                    y += h;
                }

                _showOceanData = GUI.Toggle(new Rect(x, y, w, h), _showOceanData, "Show sim data"); y += h;

                LodDataMgrAnimWaves._shapeCombinePass = GUI.Toggle(new Rect(x, y, w, h), LodDataMgrAnimWaves._shapeCombinePass, "Shape combine pass"); y += h;
                LodDataMgrAnimWaves._shapeCombinePassPingPong = GUI.Toggle(new Rect(x, y, w, h), LodDataMgrAnimWaves._shapeCombinePassPingPong, "Combine pass ping pong"); y += h;

                LodDataMgrShadow.s_processData = GUI.Toggle(new Rect(x, y, w, h), LodDataMgrShadow.s_processData, "Process Shadows"); y += h;

                if (OceanRenderer.Instance)
                {
                    if (OceanRenderer.Instance._lodDataDynWaves != null)
                    {
                        int steps; float dt;
                        OceanRenderer.Instance._lodDataDynWaves.GetSimSubstepData(OceanRenderer.Instance.DeltaTimeDynamics, out steps, out dt);
                        GUI.Label(new Rect(x, y, w, h), string.Format("Sim steps: {0:0.00000} x {1}", dt, steps)); y += h;
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

            // draw sim data
            float column = 1f;

            DrawSims<LodDataMgrAnimWaves>(OceanRenderer.Instance._lodDataAnimWaves, true, ref column);
            DrawSims<LodDataMgrDynWaves>(OceanRenderer.Instance._lodDataDynWaves, false, ref column);
            DrawSims<LodDataMgrFoam>(OceanRenderer.Instance._lodDataFoam, false, ref column);
            DrawSims<LodDataMgrFlow>(OceanRenderer.Instance._lodDataFlow, false, ref column);
            DrawSims<LodDataMgrShadow>(OceanRenderer.Instance._lodDataShadow, false, ref column);
            DrawSims<LodDataMgrSeaFloorDepth>(OceanRenderer.Instance._lodDataSeaDepths, false, ref column);
            DrawSims<LodDataMgrClipSurface>(OceanRenderer.Instance._lodDataClipSurface, false, ref column);
        }

        static void DrawSims<SimType>(LodDataMgr lodData, bool showByDefault, ref float offset) where SimType : LodDataMgr
        {
            if (lodData == null) return;

            var type = typeof(SimType);
            if (!s_drawTargets.ContainsKey(type))
            {
                s_drawTargets.Add(type, showByDefault);
            }
            if (!s_simNames.ContainsKey(type))
            {
                s_simNames.Add(type, type.Name.Substring(10));
            }

            float togglesBegin = Screen.height - _bottomPanelHeight;
            float b = 7f;
            float h = togglesBegin / (float)lodData.DataTexture.volumeDepth;
            float w = h + b;
            float x = Screen.width - w * offset + b * (offset - 1f);

            if (s_drawTargets[type])
            {
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

                        // Render specific slice of 2D texture array
                        s_textureArrayMaterial.SetInt("_Depth", idx);
                        Graphics.DrawTexture(new Rect(x + b, y + b / 2f, h - b, h - b), lodData.DataTexture, s_textureArrayMaterial);
                    }
                }
            }


            s_drawTargets[type] = GUI.Toggle(new Rect(x + b, togglesBegin, w - 2f * b, _bottomPanelHeight), s_drawTargets[type], s_simNames[type]);

            offset++;
        }

        void ToggleGUI()
        {
            _guiVisible = !_guiVisible;
        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_drawTargets.Clear();
            s_simNames.Clear();
            s_textureArrayMaterial = null;
        }
    }
}
