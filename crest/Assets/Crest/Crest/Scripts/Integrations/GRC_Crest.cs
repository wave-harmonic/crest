// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if GAIA_2023

using Crest;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    public class GRC_Crest : GaiaRuntimeComponent
    {
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
            var water = FindObjectOfType<OceanRenderer>(includeInactive: true);

            if (water == null)
            {
                water = new GameObject("Water").AddComponent<OceanRenderer>();
            }

            water.transform.position = new Vector3(0f, GaiaAPI.GetSeaLevel(), 0f);
        }

        /// Called when "Remove" is pressed.
        /// <inheritdoc/>
        public override void RemoveFromScene()
        {
            var water = FindObjectOfType<OceanRenderer>(includeInactive: true);
            if (water != null) DestroyImmediate(water.gameObject);
        }
    }
}
#endif
