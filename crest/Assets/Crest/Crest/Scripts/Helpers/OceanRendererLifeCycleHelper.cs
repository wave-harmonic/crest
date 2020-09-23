// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Enables/disables a component based on the OceanRenderer's life cycle.
    /// </summary>
    public class OceanRendererLifeCycleHelper
    {
        // The mono behaviour we want to track the enabled/disabled preference.
        readonly MonoBehaviour _monoBehaviour;
        // Store the enabled/disabled checkbox set by the user so we can respect it.
        bool _enabledUserPreference;
        // Whether the state change came from the user or from the ocean life cycle event.
        bool _isStateChangeFromUser = true;

        public OceanRendererLifeCycleHelper(MonoBehaviour monoBehaviour)
        {
            Debug.Log($"OceanRendererLifeCycleHelper {monoBehaviour}");
            _monoBehaviour = monoBehaviour;
            _enabledUserPreference = _monoBehaviour.enabled;

            OceanRenderer.OnOceanRendererEnabled -= OnOceanRendererEnabled;
            OceanRenderer.OnOceanRendererEnabled += OnOceanRendererEnabled;
            OceanRenderer.OnOceanRendererDisabled -= OnOceanRendererDisabled;
            OceanRenderer.OnOceanRendererDisabled += OnOceanRendererDisabled;
        }

        ~OceanRendererLifeCycleHelper()
        {
            OceanRenderer.OnOceanRendererEnabled -= OnOceanRendererEnabled;
            OceanRenderer.OnOceanRendererDisabled -= OnOceanRendererDisabled;
        }

        /// <summary>
        /// Sets enabled based on OceanRenderer's life cycle. Callee must return early if false is returned.
        /// </summary>
        public bool OnEnable()
        {
            if (_isStateChangeFromUser)
            {
                _enabledUserPreference = true;
            }
            else
            {
                // This change came from elsewhere. We must always clear this after a state change from elsewhere.
                _isStateChangeFromUser = true;
            }

            if (OceanRenderer.Instance == null)
            {
                // False, because we do not want OnDisable changing the preference.
                _isStateChangeFromUser = false;
#if UNITY_EDITOR
                // We do not want to change a serialised value in edit mode.
                if (Application.isPlaying)
#endif
                {
                    _monoBehaviour.enabled = false;
                }

                return false;
            }

            return true;
        }


        /// <summary>
        /// Sets enabled based on OceanRenderer's life cycle.
        /// </summary>
        public void OnDisable()
        {
            if (_isStateChangeFromUser)
            {
                _enabledUserPreference = false;
            }
            else
            {
                // This change came from elsewhere. We must always clear this after a state change from elsewhere.
                _isStateChangeFromUser = true;
            }
        }

        void OnOceanRendererEnabled()
        {
            // State change is from here which is not the user. Flag it so we know not to update the user preference.
            _isStateChangeFromUser = !_enabledUserPreference;

#if UNITY_EDITOR
            // We do not want to change a serialised value in edit mode.
            if (Application.isPlaying && _monoBehaviour != null)
#endif
            {
                _monoBehaviour.enabled = _enabledUserPreference;
            }
        }

        void OnOceanRendererDisabled()
        {
            // State change is from here which is not the user. Flag it so we know not to update the user preference.
            _isStateChangeFromUser = !_enabledUserPreference;
#if UNITY_EDITOR
            // We do not want to change a serialised value in edit mode.
            if (Application.isPlaying && _monoBehaviour != null)
#endif
            {
                _monoBehaviour.enabled = false;
            }
        }
    }
}
