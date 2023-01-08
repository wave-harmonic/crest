// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Examples
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Rendering;

    [ExecuteDuringEditMode]
    public abstract class CustomPassForCameraBase : CustomMonoBehaviour
    {
        // Use int to support other RPs. We could use a custom enum to map to each RP in the future.
        protected abstract int Event { get; }

        int _currentEvent;
        CommandBuffer _buffer;
        List<Camera> _cameras = new List<Camera>();

        void OnEnable()
        {
            if (_buffer == null)
            {
                _buffer = new CommandBuffer()
                {
                    name = "Execute Command Buffer",
                };
            }

            _currentEvent = Event;

            Camera.onPreRender -= OnBeforeRender;
            Camera.onPreRender += OnBeforeRender;
        }

        void OnDisable()
        {
            Camera.onPreRender -= OnBeforeRender;
            CleanCameras();
        }

        void CleanCameras()
        {
            foreach (var camera in _cameras)
            {
                // This can happen on recompile. Thankfully, command buffers will be removed for us.
                if (camera == null)
                {
                    continue;
                }

                foreach (CameraEvent @event in System.Enum.GetValues(typeof(CameraEvent)))
                {
                    camera.RemoveCommandBuffer(@event, _buffer);
                }

                Clear(_buffer, camera);
                Graphics.ExecuteCommandBuffer(_buffer);
            }

            _cameras.Clear();
        }

        void OnBeforeRender(Camera camera)
        {
            if (!_cameras.Contains(camera))
            {
                if (_currentEvent != Event)
                {
                    CleanCameras();
                    _currentEvent = Event;
                }

                _cameras.Add(camera);
                camera.AddCommandBuffer((CameraEvent)Event, _buffer);
            }

            // Buffer will be shared by multiple cameras and multiple Execute calls. Clear once for each camera.
            _buffer.Clear();

            // Only execute for main camera and editor only cameras.
            if (Camera.main != camera && camera.cameraType != CameraType.SceneView && !camera.name.StartsWithNoAlloc("Preview"))
            {
                Clear(_buffer, camera);
                return;
            }

            Execute(_buffer, camera, XRHelpers.GetRenderTextureDescriptor(camera));
        }

        protected abstract void Execute(CommandBuffer buffer, Camera camera, RenderTextureDescriptor descriptor);
        protected abstract void Clear(CommandBuffer buffer, Camera camera);
    }

    public class CustomPassForCamera : CustomPassForCameraBase
    {
        // TODO: We need a separate event for deferred as it is very different from forward. But having an event per
        // camera complicates things so will skip for now. Will probably need a dictionary.
        [SerializeField]
        CameraEvent _event;

        [SerializeField]
        UnityEvent<CommandBuffer, Camera, RenderTextureDescriptor> _onExecute = new UnityEvent<CommandBuffer, Camera, RenderTextureDescriptor>();

        [SerializeField]
        UnityEvent<CommandBuffer, Camera> _onClear = new UnityEvent<CommandBuffer, Camera>();

        protected override int Event
        {
            get
            {
                return (int)_event;
            }
        }

        protected override void Execute(CommandBuffer buffer, Camera camera, RenderTextureDescriptor descriptor)
        {
            _onExecute.Invoke(buffer, camera, descriptor);
        }

        protected override void Clear(CommandBuffer buffer, Camera camera)
        {
            _onClear.Invoke(buffer, camera);
        }
    }
}
