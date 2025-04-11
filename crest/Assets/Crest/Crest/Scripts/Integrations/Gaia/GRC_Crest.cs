// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    static class Helper
    {
        internal static T FindObjectByType<T>(bool includeInactive = false) where T : Object
        {
#if UNITY_2023_3_OR_NEWER
            return Object.FindFirstObjectByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
            return Object.FindObjectOfType<T>(includeInactive);
#endif
        }

        internal static T[] FindObjectsByType<T>(bool includeInactive = false) where T : Object
        {
#if UNITY_2023_3_OR_NEWER
            return Object.FindObjectsByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>(includeInactive);
#endif
        }
    }

    public class GRC_Crest : GaiaRuntimeComponent
    {
        public bool _wind = true;
        public bool _swell = true;
        public bool _underwater = true;

        GUIContent _helpLink;

        /// <inheritdoc/>
        public override GUIContent PanelLabel
        {
            get
            {
                if (_panelLabel == null || _panelLabel.text == "")
                {
                    _panelLabel = new GUIContent("Crest Water", "Adds Crest Water to your scene.");
                }

                return _panelLabel;
            }
        }
        GUIContent _panelLabel;


        /// <inheritdoc/>
        public override void Initialize()
        {
            // Order components appear in the UI. Try to keep in alphabetical order.
            m_orderNumber = 210;

            if (_helpLink == null || _helpLink.text == "")
            {
                _helpLink = new GUIContent("Crest Online documentation", "Opens the documentation for the Crest Water System in your browser.");
            }

        }

        /// <inheritdoc/>
        public override void DrawUI()
        {
            // Displays "?" help button.
            DisplayHelp
            (
                "This module adds the Crest Water System to your scene. Please visit the link to learn more:",
                _helpLink,
                "https://crest.readthedocs.io/en/stable/about/introduction.html"
            );

            EditorGUI.BeginChangeCheck();
            {
                _swell = EditorGUILayout.Toggle("Swell Waves", _swell);
                DisplayHelp("Whether to add swell waves to the scene. Modify the component after creation to customize.");

                _wind = EditorGUILayout.Toggle("Wind Waves", _wind);
                DisplayHelp("Whether to add wind waves to the scene. Requires Gaia's Wind Zone. Modify the component after creation to customize.");

                _underwater = EditorGUILayout.Toggle("Underwater", _underwater);
                DisplayHelp("Whether to add underwater rendering to the scene. Modify the component after creation to customize.");

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove"))
                {
                    RemoveFromScene();
                }
                GUILayout.Space(15);
                if (GUILayout.Button("Apply"))
                {
                    AddToScene();
                }
                GUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(this);
            }
        }

        /// Called when either "Apply" or "Create Runtime" is pressed.
        /// <inheritdoc/>
        public override void AddToScene()
        {
            // Re-initialize to keep user's changes.
            var water = Helper.FindObjectByType<OceanRenderer>(includeInactive: true);

            if (water == null)
            {
                water = new GameObject("Water").AddComponent<OceanRenderer>();
            }

            water.transform.position = new Vector3(0f, GaiaAPI.GetSeaLevel(), 0f);

            var managed = water.transform.Find("Managed");

            if (managed == null)
            {
                managed = new GameObject("Managed").transform;
            }

            managed.SetParent(water.transform, worldPositionStays: false);

            // Wind
            if (_wind)
            {
                var wind = Helper.FindObjectByType<WindManager>();

                if (wind != null)
                {
                    water._globalWindZone = wind.GetComponent<WindZone>();
                }
            }

            // Depth
            foreach (var terrain in Helper.FindObjectsByType<Terrain>(includeInactive: true))
            {
                var depthCache = terrain.GetComponentInChildren<OceanDepthCache>(includeInactive: true);

                if (depthCache == null)
                {
                    depthCache = new GameObject("WaterDepthCache").AddComponent<OceanDepthCache>();
                }

                depthCache.gameObject.layer = water.Layer;
                depthCache.transform.SetParent(terrain.transform, worldPositionStays: false);
                depthCache.transform.localPosition = terrain.terrainData.size * 0.5f;
                var position = depthCache.transform.position;
                position.y = GaiaAPI.GetSeaLevel();
                depthCache.transform.position = position;
                depthCache.transform.localScale = new(terrain.terrainData.size.x, 1f, terrain.terrainData.size.z);
                depthCache._layers = 1 << terrain.gameObject.layer;
                depthCache._cameraMaxTerrainHeight = terrain.terrainData.size.y * 0.5f;
                depthCache._cameraFarClipPlane = terrain.terrainData.size.y;
                depthCache._resolution = terrain.terrainData.heightmapResolution - 1;
                depthCache.PopulateCache(updateComponents: true);
            }

            // Wind Waves
            if (_wind && water._globalWindZone != null)
            {
                var waves = managed.GetComponentInChildren<ShapeFFT>();

                if (waves == null)
                {
                    waves = new GameObject("WaterWindWaves").AddComponent<ShapeFFT>();
                }

                waves.transform.SetParent(managed.transform, worldPositionStays: false);
            }
            else
            {
                var waves = managed.GetComponentInChildren<ShapeFFT>();

                if (waves != null)
                {
                    DestroyImmediate(waves.gameObject);
                }
            }

            // Swell Waves
            if (_swell)
            {
                var waves = managed.GetComponentInChildren<ShapeGerstner>();

                if (waves == null)
                {
                    waves = new GameObject("WaterSwellWaves").AddComponent<ShapeGerstner>();
                }

                waves.transform.SetParent(managed.transform, worldPositionStays: false);
                waves._overrideGlobalWindDirection = true;
                waves._overrideGlobalWindSpeed = true;
                waves._reverseWaveWeight = 0;
                waves._swell = true;
            }
            else
            {
                var waves = managed.GetComponentInChildren<ShapeGerstner>();

                if (waves != null)
                {
                    DestroyImmediate(waves.gameObject);
                }
            }

            if (Camera.main != null)
            {
                var camera = Camera.main;

                if (!camera.TryGetComponent<UnderwaterRenderer>(out var underwater))
                {
                    underwater = camera.gameObject.AddComponent<UnderwaterRenderer>();
                }
            }
        }

        /// Called when "Remove" is pressed.
        /// <inheritdoc/>
        public override void RemoveFromScene()
        {
            var water = Helper.FindObjectByType<OceanRenderer>(includeInactive: true);
            if (water != null) DestroyImmediate(water.gameObject);

            foreach (var terrain in Helper.FindObjectsByType<Terrain>(includeInactive: true))
            {
                var depthCache = terrain.GetComponentInChildren<OceanDepthCache>(includeInactive: true);
                if (depthCache != null) DestroyImmediate(depthCache.gameObject);
            }

            if (Camera.main != null)
            {
                var camera = Camera.main;

                if (camera.TryGetComponent<UnderwaterRenderer>(out var underwater))
                {
                    DestroyImmediate(underwater);
                }
            }
        }
    }
}
