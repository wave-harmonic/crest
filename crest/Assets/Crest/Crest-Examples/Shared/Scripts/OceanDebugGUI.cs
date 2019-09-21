// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class OceanDebugGUI : MonoBehaviour
{
    [SerializeField] bool _showSimTargets = false;
    [SerializeField] bool _guiVisible = true;
    static float _leftPanelWidth = 180f;
    ShapeGerstnerBatched[] _gerstners;

    static Dictionary<System.Type, bool> _drawTargets = new Dictionary<System.Type, bool>();
    static Dictionary<System.Type, string> _simNames = new Dictionary<System.Type, string>();

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

            GUI.color = Color.black * 0.7f;
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
                // which helps if the gerstners are on sibling GOs.
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

            _showSimTargets = GUI.Toggle(new Rect(x, y, w, h), _showSimTargets, "Show sim data"); y += h;

            LodDataMgrAnimWaves._shapeCombinePass = GUI.Toggle(new Rect(x, y, w, h), LodDataMgrAnimWaves._shapeCombinePass, "Shape combine pass"); y += h;
            LodDataMgrAnimWaves._shapeCombinePassPingPong = GUI.Toggle(new Rect(x, y, w, h), LodDataMgrAnimWaves._shapeCombinePassPingPong, "Combine pass ping pong"); y += h;

            LodDataMgrShadow.s_processData = GUI.Toggle(new Rect(x, y, w, h), LodDataMgrShadow.s_processData, "Process Shadows"); y += h;

            if (GPUReadbackDisps.Instance)
            {
                int count, min, max;
                GPUReadbackDisps.Instance.GetStats(out count, out min, out max);

#if UNITY_EDITOR
                GPUReadbackDisps.Instance._doReadback = GUI.Toggle(new Rect(x, y, w, h), GPUReadbackDisps.Instance._doReadback, "Readback coll data"); y += h;
#endif
                // generates garbage
                GUI.Label(new Rect(x, y, w, h), string.Format("Coll Texture Count: {0}", count)); y += h;
                GUI.Label(new Rect(x, y, w, h), string.Format("Coll Queue Lengths: [{0}, {1}]", min, max)); y += h;
            }

            if (OceanRenderer.Instance)
            {
                if (OceanRenderer.Instance._simSettingsAnimatedWaves.CachedHeightQueries)
                {
                    var cache = OceanRenderer.Instance.CollisionProvider as CollProviderCache;
                    // generates garbage
                    GUI.Label(new Rect(x, y, w, h), string.Format("Cache hits: {0}/{1}", cache.CacheHits, cache.CacheChecks)); y += h;
                }

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
        if (_showSimTargets)
        {
            DrawShapeTargets();
        }

        GUI.color = bkp;
    }

    void DrawShapeTargets()
    {
        if (OceanRenderer.Instance == null) return;

        // draw sim data
        float column = 1f;

        DrawSims<LodDataMgrAnimWaves>(OceanRenderer.Instance._lodDataAnimWaves, true, ref column);
        DrawSims<LodDataMgrDynWaves>(OceanRenderer.Instance._lodDataDynWaves, false, ref column);
        DrawSims<LodDataMgrFoam>(OceanRenderer.Instance._lodDataFoam, false, ref column);
        DrawSims<LodDataMgrFlow>(OceanRenderer.Instance._lodDataFlow, false, ref column);
        DrawSims<LodDataMgrShadow>(OceanRenderer.Instance._lodDataShadow, false, ref column);
        DrawSims<LodDataMgrSeaFloorDepth>(OceanRenderer.Instance._lodDataSeaDepths, false, ref column);
    }

    static Dictionary<RenderTextureFormat, RenderTexture> shapes = new Dictionary<RenderTextureFormat, RenderTexture>();

    static void DrawSims<SimType>(LodDataMgr lodData, bool showByDefault, ref float offset) where SimType : LodDataMgr
    {
        if (lodData == null) return;

        var type = typeof(SimType);
        if (!_drawTargets.ContainsKey(type))
        {
            _drawTargets.Add(type, showByDefault);
        }
        if (!_simNames.ContainsKey(type))
        {
            _simNames.Add(type, type.Name.Substring(10));
        }

        float b = 7f;
        float h = Screen.height / (float)lodData.DataTexture.volumeDepth;
        float w = h + b;
        float x = Screen.width - w * offset + b * (offset - 1f);

        if (_drawTargets[type])
        {
            for (int idx = 0; idx < lodData.DataTexture.volumeDepth; idx++)
            {
                float y = idx * h;
                if (offset == 1f) w += b;

                // We cannot debug draw texture arrays directly
                // (unless we write our own system for doing so).
                // So for now, we just copy each texture and then draw that.
                if (!shapes.ContainsKey(lodData.DataTexture.format))
                {
                    var rt = new RenderTexture(lodData.DataTexture);
                    rt.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
                    rt.Create();
                    shapes.Add(lodData.DataTexture.format, rt);
                }

                RenderTexture shape = shapes[lodData.DataTexture.format];
                Graphics.CopyTexture(lodData.DataTexture, idx, 0, shape, 0, 0);

                GUI.color = Color.black * 0.7f;
                GUI.DrawTexture(new Rect(x, y, w - b, h), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(x + b, y + b / 2f, h - b, h - b), shape, ScaleMode.ScaleAndCrop, false);
            }
        }

        _drawTargets[type] = GUI.Toggle(new Rect(x + b, Screen.height - 25f, w - 2f * b, 25f), _drawTargets[type], _simNames[type]);

        offset++;
    }

    void ToggleGUI()
    {
        _guiVisible = !_guiVisible;
    }
}
