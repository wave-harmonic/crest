// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace Crest
{
    /// <summary>
    /// This time provider feeds a Timeline time to the ocean system, using a Playable Director
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Cutscene Time Provider")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "time-providers.html" + Internal.Constants.HELP_URL_RP + "#timelines-and-cutscenes")]
    public class TimeProviderCutscene : TimeProviderBase
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Tooltip("Playable Director to take time from"), SerializeField]
        PlayableDirector _playableDirector;

        [Tooltip("Time offset which will be added to the Timeline time"), SerializeField]
        float _timeOffset = 0f;

        [Tooltip("Auto-assign this time provider to the ocean system when this component becomes active"), SerializeField]
        bool _assignToOceanComponentOnEnable = true;

        [Tooltip("Restore whatever time provider was previously assigned to ocean system when this component disables"), SerializeField]
        bool _restorePreviousTimeProviderOnDisable = true;

        TimeProviderDefault _fallbackTP = new TimeProviderDefault();

        bool _initialised = false;

        private void OnStart()
        {
            Initialise(true);
        }

        void Initialise(bool showErrors)
        {
            if (OceanRenderer.Instance == null)
            {
                if (showErrors)
                {
                    Debug.LogError("Crest: No ocean present, TimeProviderCutscene will have no effect.", this);
                }
#if !UNITY_EDITOR
                enabled = false;
#endif
                return;
            }

            if (_playableDirector == null)
            {
                if (showErrors)
                {
                    Debug.LogError("Crest: No Playable Director component assigned, TimeProviderCutscene will have no effect.", this);
                }
#if !UNITY_EDITOR
                enabled = false;
#endif
                return;
            }

            if (_assignToOceanComponentOnEnable)
            {
                OceanRenderer.Instance.PushTimeProvider(this);
            }

            _initialised = true;
        }

        private void OnDisable()
        {
            if (_restorePreviousTimeProviderOnDisable && OceanRenderer.Instance != null && _initialised)
            {
                OceanRenderer.Instance.PopTimeProvider(this);
            }

            _initialised = false;
        }

#if UNITY_EDITOR
        // Needed to keep up to date while editing. Don't want to display errors when user
        // is halfway through configuring component, and keep trying to initialise until
        // everything is there.
        private void Update()
        {
            if (!_initialised)
            {
                Initialise(false);
            }
        }
#endif

        // If there is a playable director which is playing, return its time, otherwise
        // use whatever TP was being used before this component initialised, else fallback
        // to a default TP.
        public override float CurrentTime
        {
            get
            {
                if (_playableDirector != null
                    && _playableDirector.isActiveAndEnabled
                    && (!Application.isPlaying || _playableDirector.state == PlayState.Playing))
                {
                    return (float)_playableDirector.time + _timeOffset;
                }

                // Use a fallback TP
                return _fallbackTP.CurrentTime;
            }
        }

        public override float DeltaTime => Time.deltaTime;
        public override float DeltaTimeDynamics => DeltaTime;
    }
}
