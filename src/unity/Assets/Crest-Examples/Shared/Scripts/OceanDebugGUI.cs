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
    ShapeGerstnerBatched[] gerstners;

    static Dictionary<System.Type, bool> _drawTargets = new Dictionary<System.Type, bool>();
    static Dictionary<System.Type, string> _simNames = new Dictionary<System.Type, string>();

    public static bool OverGUI( Vector2 screenPosition )
    {
        return screenPosition.x < _leftPanelWidth;
    }

    private void Start()
    {
        if (OceanRenderer.Instance == null)
        {
            enabled = false;
            return;
        }

        gerstners = FindObjectsOfType<ShapeGerstnerBatched>();
        // i am getting the array in the reverse order compared to the hierarchy which bugs me. sort them based on sibling index,
        // which helps if the gerstners are on sibling GOs.
        System.Array.Sort(gerstners, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
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
        if( Input.GetKeyDown(KeyCode.R))
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
            foreach (var gerstner in gerstners)
            {
                gerstner._weight = GUI.HorizontalSlider(new Rect(x, y, w, h), gerstner._weight, 0f, 1f); y += h;
            }

            _showSimTargets = GUI.Toggle(new Rect(x, y, w, h), _showSimTargets, "Show sim data"); y += h;

#if UNITY_EDITOR
            LodDataMgrAnimWaves._shapeCombinePass = GUI.Toggle(new Rect(x, y, w, h), LodDataMgrAnimWaves._shapeCombinePass, "Shape combine pass"); y += h;
#endif

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

            if (OceanRenderer.Instance._simSettingsAnimatedWaves.CachedHeightQueries)
            {
                var cache = OceanRenderer.Instance.CollisionProvider as CollProviderCache;
                // generates garbage
                GUI.Label(new Rect(x, y, w, h), string.Format("Cache hits: {0}/{1}", cache.CacheHits, cache.CacheChecks)); y += h;
            }

            if (GUI.Button(new Rect(x, y, w, h), "Hide GUI (G)"))
            {
                ToggleGUI();
            }
            y += h;

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

        // draw source textures to screen
        if ( _showSimTargets )
        {
            DrawShapeTargets();
        }

        GUI.color = bkp;
    }

    void DrawShapeTargets()
    {
        // draw sim data
        float column = 1f;

        DrawSims<LodDataMgrAnimWaves>(OceanRenderer.Instance._lodDataAnimWaves, true, ref column);
        if (OceanRenderer.Instance._createDynamicWaveSim) DrawSims<LodDataMgrDynWaves>(OceanRenderer.Instance._lodDataDynWaves, false, ref column);
        if (OceanRenderer.Instance._createFoamSim) DrawSims<LodDataMgrFoam>(OceanRenderer.Instance._lodDataFoam, false, ref column);
        if (OceanRenderer.Instance._createFlowSim) DrawSims<LodDataMgrFlow>(OceanRenderer.Instance._lodDataFlow, false, ref column);
        if (OceanRenderer.Instance._createShadowData) DrawSims<LodDataMgrShadow>(OceanRenderer.Instance._lodDataShadow, false, ref column);
        DrawSims<LodDataMgrSeaFloorDepth>(OceanRenderer.Instance._lodDataSeaDepths, false, ref column);
    }

    static void DrawSims<SimType>(LodDataMgr lodData, bool showByDefault, ref float offset) where SimType : LodDataMgr
    {
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
        float h = Screen.height / (float)OceanRenderer.Instance._lods.Length;
        float w = h + b;
        float x = Screen.width - w * offset + b * (offset - 1f);

        if (_drawTargets[type])
        {
            for (int idx = 0; idx < OceanRenderer.Instance.CurrentLodCount; idx++)
            {
                float y = idx * h;
                if (offset == 1f) w += b;

                RenderTexture shape;

                shape = lodData.DataTexture(idx);
                if (shape == null) continue;

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
