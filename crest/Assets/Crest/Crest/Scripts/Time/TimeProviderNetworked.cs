// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Gives a time to Crest with a custom time offset. Assign this component to the Ocean
    /// Renderer component and set the TimeOffsetToServer property of this component to the
    /// delta from this client's time to the shared server time.
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Networked Time Provider")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "time-providers.html" + Internal.Constants.HELP_URL_RP + "#network-synchronisation")]
    public class TimeProviderNetworked : TimeProviderBase
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        /// <summary>
        /// If Time.time on this client is 1.5s ahead of the shared/server Time.time, set
        /// this field to -1.5.
        /// </summary>
        public float TimeOffsetToServer { get; set; }

        TimeProviderDefault _tpDefault = new TimeProviderDefault();

        public override float CurrentTime => _tpDefault.CurrentTime + TimeOffsetToServer;
        public override float DeltaTime => _tpDefault.DeltaTime;
        public override float DeltaTimeDynamics => DeltaTime;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TimeProviderNetworked))]
    class TimeProviderNetworkedEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Assign this component to the Ocean Renderer component and set the TimeOffsetToServer property of this component (at runtime from C#) to the delta from this client's time to the shared server time.", MessageType.Info);
        }
    }
#endif // UNITY_EDITOR
}
