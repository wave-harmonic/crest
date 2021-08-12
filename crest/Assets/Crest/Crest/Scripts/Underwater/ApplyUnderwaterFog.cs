// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using System;
    using System.Reflection;
    using UnityEngine;

    public static class Extensions
    {
        // Taken from:
        // http://answers.unity3d.com/questions/530178/how-to-get-a-component-from-an-object-and-add-it-t.html
        public static T GetCopyOf<T>(this Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }

        public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
        {
            return go.AddComponent<T>().GetCopyOf(toAdd) as T;
        }
    }

    [DefaultExecutionOrder(500)]
    public class ApplyUnderwaterFog : MonoBehaviour
    {
        [SerializeField]
        bool _highQuality;

        static Material s_Material;
        static Material s_MaterialHighQuality;

        Renderer _rendererWithoutFog;
        Renderer _rendererWithFog;

        MaterialPropertyBlock _materialPropertyBlock;

        void Awake()
        {
            if (s_Material == null)
            {
                s_Material = new Material(Shader.Find("Hidden/Crest/Underwater/Apply Underwater Fog"));
                s_MaterialHighQuality = new Material(Shader.Find("Hidden/Crest/Underwater/Apply Underwater Fog HQ"));
            }

            CreateFoggedDuplicated();
            _materialPropertyBlock = new MaterialPropertyBlock();
        }


        // Issues:
        // - Duplicating a particle system is expensive.
        // - Does not work with randomisation.
        void CreateFoggedDuplicated()
        {
            var foggedGameObject = new GameObject("Fogged Duplicate");
            foggedGameObject.transform.SetParent(transform, worldPositionStays: false);

            foggedGameObject.transform.localPosition = Vector3.zero;
            foggedGameObject.transform.localScale = Vector3.one * 1.000001f;
            foggedGameObject.transform.rotation = Quaternion.identity;

            _rendererWithoutFog = GetComponent<Renderer>();

            if (TryGetComponent<MeshRenderer>(out _))
            {
                _rendererWithFog = foggedGameObject.AddComponent<MeshRenderer>();
                var mf = foggedGameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = GetComponent<MeshFilter>().sharedMesh;
            }
            else if (TryGetComponent<ParticleSystemRenderer>(out _))
            {
                var particleSystem = GetComponent<ParticleSystem>();
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var foggedParticleSystem = foggedGameObject.AddComponent(particleSystem);
                foggedGameObject.transform.localScale = Vector3.one;

                if (particleSystem.main.playOnAwake)
                {
                    particleSystem.Play(true);
                    foggedParticleSystem.Play(true);
                }

                _rendererWithFog = foggedGameObject.GetComponent<Renderer>();
            }

            _rendererWithFog.material = _highQuality ? s_MaterialHighQuality : s_Material;
        }

        // Issues:
        // - MeshRenderer will show a warning that there are more materials than submeshes.
        // - Does not work with particles.
        void AddFoggedMaterial()
        {
            _rendererWithoutFog = GetComponent<Renderer>();
            _rendererWithFog = _rendererWithoutFog;

            // Copy material list.
            var materials = new Material[_rendererWithoutFog.sharedMaterials.Length + 1];
            for (var i = 0; i < _rendererWithoutFog.sharedMaterials.Length; i++)
            {
                materials[i] = _rendererWithoutFog.sharedMaterials[i];
            }

            // Add fogged material.
            materials[materials.Length - 1] = s_Material;

            _rendererWithoutFog.sharedMaterials = materials;
        }

        void OnEnable()
        {
            Camera.onPreRender -= OnCameraPreRender;
            Camera.onPreRender += OnCameraPreRender;
        }

        void OnDisable()
        {
            Camera.onPreRender -= OnCameraPreRender;
        }

        void OnCameraPreRender(Camera camera)
        {
            if (camera != OceanRenderer.Instance.ViewCamera) return;
            s_Material.CopyPropertiesFromMaterial(UnderwaterRenderer.Instance._underwaterEffectMaterial.material);
            s_MaterialHighQuality.CopyPropertiesFromMaterial(UnderwaterRenderer.Instance._underwaterEffectMaterial.material);

            var texture = _rendererWithoutFog.sharedMaterials[0].HasProperty("_MainTex") ? _rendererWithoutFog.sharedMaterials[0].GetTexture("_MainTex") : null;
            if (texture != null)
            {
                _materialPropertyBlock.SetTexture("_MainTex", _rendererWithoutFog.sharedMaterials[0].GetTexture("_MainTex"));
                _materialPropertyBlock.SetVector("_MainTex_ST", _rendererWithoutFog.sharedMaterials[0].GetVector("_MainTex_ST"));
            }
            else
            {
                _materialPropertyBlock.SetTexture("_MainTex", Texture2D.whiteTexture);
            }
            _rendererWithFog.SetPropertyBlock(_materialPropertyBlock);
        }
    }
}
