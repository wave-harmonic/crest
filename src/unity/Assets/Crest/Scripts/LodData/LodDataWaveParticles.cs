// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A persistent flow simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataWaveParticles : MonoBehaviour
    {
        public Material convolutionMaterial;

        private WaveParticlesSystem _waveParticlesSystem = new WaveParticlesSystem();
        private ExtendedHeightField _heightField = new ExtendedHeightField(8, 8, 1024, 1024);
        private int _frame = 0;
        private RenderTexture _finalTexture;
        private Texture2D _convolutionKernel;

        public static Color[] CreateKernel(int kernelHeight, int kernelWidth, ExtendedHeightField.HeightFieldInfo heightFieldInfo)
        {
            Color[] kernel = new Color[kernelWidth * kernelHeight];

            // Create the kernel that is used in convolution.
            for (int y = 0; y < kernelHeight; y++)
            {
                for (int x = 0; x < kernelWidth; x++)
                {
                    int index = (y * kernelWidth) + x;
                    float abs_diff;

                    float x_component = ((kernelWidth / 2) - x) * heightFieldInfo.UnitX;
                    float y_component = ((kernelHeight / 2) - y) * heightFieldInfo.UnitY;
                    {
                        abs_diff = Mathf.Sqrt((y_component * y_component) + (x_component * x_component));
                    }
                    if (abs_diff > WaveParticle.RADIUS)
                    {
                        // Don't need to do rest of calculation, as these pixel fall outside of wave particles's radii.
                        kernel[index] = new Vector4(0, 0, 0, 1);
                    }
                    else
                    {
                        float relativePixelDistance = (Mathf.PI * abs_diff) / WaveParticle.RADIUS;
                        float y_displacement_factor = 0.5f * (Mathf.Cos(relativePixelDistance) + 1);
                        Vector2 long_component = -Mathf.Sqrt(2) * y_displacement_factor * Mathf.Sin(relativePixelDistance) * new Vector2(x_component, y_component);
                        kernel[index] = new Color(long_component.x, y_displacement_factor, long_component.y, 1);
                    }
                }
            }
            return kernel;
        }

        void Start() {
                _waveParticlesSystem.Initialise(500000, 0.001f);
                MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                _finalTexture = new RenderTexture(_heightField.heightFieldInfo.HoriRes, _heightField.heightFieldInfo.VertRes, 24, RenderTextureFormat.ARGBFloat);
                _finalTexture.antiAliasing = 1;
                _finalTexture.anisoLevel = 0;
                _finalTexture.autoGenerateMips = false;
                _finalTexture.wrapMode = TextureWrapMode.Clamp;
                _finalTexture.filterMode = FilterMode.Point;
                _finalTexture.name = "Final Texture";
                _finalTexture.Create();
                meshRenderer.material.SetTexture("_MainTex", _finalTexture);

                int kernelWidth = Mathf.CeilToInt((WaveParticle.RADIUS / _heightField.heightFieldInfo.Width) * _heightField.heightFieldInfo.HoriRes);
                int  kernelHeight = Mathf.CeilToInt((WaveParticle.RADIUS / _heightField.heightFieldInfo.Height) * _heightField.heightFieldInfo.VertRes);
                Color[] kernelArray = CreateKernel(kernelHeight, kernelWidth, _heightField.heightFieldInfo);
                _convolutionKernel = new Texture2D(kernelWidth, kernelHeight, TextureFormat.RGBAFloat, false);
                _convolutionKernel.name = "Convolution Kernel";
                _convolutionKernel.SetPixels(kernelArray);
                _convolutionKernel.Apply();
                convolutionMaterial.SetTexture(Shader.PropertyToID("_KernelTex"), _convolutionKernel);
                convolutionMaterial.SetFloat(Shader.PropertyToID("_Width"), _heightField.heightFieldInfo.Width);
                convolutionMaterial.SetFloat(Shader.PropertyToID("_Height"), _heightField.heightFieldInfo.Height);
                convolutionMaterial.SetInt(Shader.PropertyToID("_HoriRes"), _heightField.heightFieldInfo.HoriRes);
                convolutionMaterial.SetInt(Shader.PropertyToID("_VertRes"), _heightField.heightFieldInfo.VertRes);
                convolutionMaterial.SetFloat(Shader.PropertyToID("_ParticleRadii"), WaveParticle.RADIUS);
                convolutionMaterial.SetFloat(Shader.PropertyToID("_KernelWidth"), kernelWidth);
                convolutionMaterial.SetFloat(Shader.PropertyToID("_KernelHeight"), kernelHeight);
        }

        bool doneFrame = false;
        void Update() {
            if(doneFrame) return;
            //doneFrame = true;
            RenderTexture.active = _heightField.textureHeightMap;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;

            // Create a small ripple of particles, as it has a dispersion angle of 360
            if(_frame % 3 == 0) {
                float x = Random.Range(0f, 2f);
                if(x > 1) x = x + 7; else x = -x;
                float y = Random.Range(0f, 2f);
                if(y > 1) y = y + 7; else y = -y;
                WaveParticle waveParticle = WaveParticle.createWaveParticle(new Vector2(x, y), new Vector2(0.5f, 0.5f), 10f, Mathf.PI * 2, _frame);
                _waveParticlesSystem.addParticle(waveParticle);
            }
            _waveParticlesSystem.commitParticles();
            _waveParticlesSystem.calculateSubdivisions(_frame);
            _waveParticlesSystem.splatParticles(_frame, ref _heightField);

            RenderTexture.active = _finalTexture;
            GL.Clear(true, true, Color.black);
            Graphics.Blit(_heightField.textureHeightMap, _finalTexture, convolutionMaterial);
            _frame = (_frame + 1) % WaveParticle.FRAME_CYCLE_LENGTH;
        }

        void FixedUpdate() {
            doneFrame = false;
        }

    }
}
