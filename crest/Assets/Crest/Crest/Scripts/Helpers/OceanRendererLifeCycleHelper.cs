// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using UnityEngine;

    /// <summary>
    /// Enables/disables a component based on the OceanRenderer's life cycle. It will allow a component that depend on
    /// the OceanRenderer to initialise after the OceanRenderer has initialised (i.e. delayed initialisation).
    ///
    /// It stores the "enabled" state set by the user as it enables or disables the component if the OceanRenderer is
    /// enabled or disabled respectively using event subscription broadcasted from the OceanRenderer.
    ///
    /// It supports [ExecuteAlways/InEditMode] by storing the "enabled" state, but does not enable or disable the
    /// component as that would change serialized data.
    ///
    /// Components must call OnEnable, OnDisable and OnDestroy from their MonoBehaviour counterparts.
    /// </summary>
    public class OceanRendererLifeCycleHelper
    {
        // The mono behaviour we want to track the enabled/disabled preference.
        readonly MonoBehaviour _monoBehaviour;

        // Store the "enabled" state set by the user so we can respect it.
        internal bool _enabledValueSetByTheUser;

        // Whether the state change came from the user or from the ocean life cycle event. Generally, this should always
        // be true since state changes from users are unknown.
        bool _isStateChangeFromUser = false;

        public OceanRendererLifeCycleHelper(MonoBehaviour monoBehaviour)
        {
            _monoBehaviour = monoBehaviour;
            // Store the "enabled" state set by the user.
            _enabledValueSetByTheUser = _monoBehaviour.enabled;

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
        /// Needs to be called to clean up resources. Destructors do not work when domain reload is disabled.
        /// </summary>
        public void OnDestroy()
        {
            OceanRenderer.OnOceanRendererEnabled -= OnOceanRendererEnabled;
            OceanRenderer.OnOceanRendererDisabled -= OnOceanRendererDisabled;
        }

        /// <summary>
        /// Sets enabled based on OceanRenderer's life cycle. Callee must return early if false is returned.
        /// </summary>
        public bool OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // In edit mode just store "enabled" as we do not disable the component anyway.
                _enabledValueSetByTheUser = _monoBehaviour.enabled;
                return OceanRenderer.Instance != null;
            }
#endif

            if (_isStateChangeFromUser)
            {
                // Flip value as it could be either the user enabling or disabling the component. For the case where the
                // ocean renderer is not active and this component is set to be enabled, the component will be disabled.
                // The user will see an unchecked checkbox for "enable" as they should. When they toggle it, it will
                // enable the component but since the ocean is disabled it will disable itself after recording the
                // user's preference.
                _enabledValueSetByTheUser =  !_enabledValueSetByTheUser;
            }

            // Always revert to "true" as whether the change comes from a user is unknown.
            _isStateChangeFromUser = true;

            if (OceanRenderer.Instance == null)
            {
                // Mark "false" as we will now disable the component and the subsequent OnDisable is not from the user.
                _isStateChangeFromUser = false;
                _monoBehaviour.enabled = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets enabled based on OceanRenderer's life cycle.
        /// </summary>
        public void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // In edit mode just store "enabled" as we do not disable the component anyway.
                _enabledValueSetByTheUser = _monoBehaviour.enabled;
                return;
            }
#endif

            if (_isStateChangeFromUser)
            {
                _enabledValueSetByTheUser = false;
            }

            // Always revert to "true" as whether the change comes from a user is unknown.
            _isStateChangeFromUser = true;
        }

        // This will be called by the OceanRenderer through an event. If during the component's OnEnable event the
        // OceanRenderer is already ready, then this will be skipped.
        void OnOceanRendererEnabled()
        {
#if UNITY_EDITOR
            // We do not want to change a serialised value in edit mode.
            if (!Application.isPlaying)
            {
                return;
            }
#endif

            // If these two are equal then this event will do nothing and next OnEnable/OnDisable could be the user.
            _isStateChangeFromUser = _monoBehaviour.enabled == _enabledValueSetByTheUser;
            // Apply the "enabled" state that the user wants now that OceanRenderer is ready.
            _monoBehaviour.enabled = _enabledValueSetByTheUser;
        }

        // This will be called by the OceanRenderer through an event.
        void OnOceanRendererDisabled()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            // If the user has set the component to disabled, then this event will do nothing and next
            // OnEnable/OnDisable could be the user.
            _isStateChangeFromUser = _enabledValueSetByTheUser == false;
            _monoBehaviour.enabled = false;
        }
    }
}
