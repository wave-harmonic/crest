// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Examples
{
    using UnityEngine;
    using UnityEngine.Events;

    public class SendUnityEvent : MonoBehaviour
    {
        [SerializeField]
        UnityEvent _onEnable = new UnityEvent();

        [SerializeField]
        UnityEvent _onDisable = new UnityEvent();

        [SerializeField]
        UnityEvent<float> _onUpdate = new UnityEvent<float>();

        float _timeSinceEnabled;

        void OnEnable()
        {
            _timeSinceEnabled = 0f;
            _onEnable.Invoke();
        }

        void OnDisable()
        {
            _onDisable.Invoke();
        }

        void Update()
        {
            _timeSinceEnabled += Time.deltaTime;
            _onUpdate.Invoke(_timeSinceEnabled);
        }
    }
}
