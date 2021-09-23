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
        UnityEvent _onUpdate = new UnityEvent();

        void OnEnable() => _onEnable.Invoke();
        void OnDisable() => _onDisable.Invoke();
        void Update() => _onUpdate.Invoke();
    }
}
