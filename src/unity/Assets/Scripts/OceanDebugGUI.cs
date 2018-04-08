using UnityEngine;

namespace Crest
{
    public class OceanDebugGUI : MonoBehaviour
    {
        public bool _showSimTargets = true;

        static float _leftPanelWidth = 180f;

        public bool _guiVisible = true;

        public static bool OverGUI( Vector2 screenPosition )
        {
            return screenPosition.x < _leftPanelWidth;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                ToggleGUI();
            }
        }

        void OnGUI()
        {
            Color bkp = GUI.color;

            if (_guiVisible)
            {
                GUI.skin.toggle.normal.textColor = Color.white;
                GUI.skin.label.normal.textColor = Color.white;

                float x = 5f, y = 0f;
                float w = _leftPanelWidth - 2f * x, h = 25f;

                GUI.color = Color.black * 0.7f;
                GUI.DrawTexture(new Rect(0, 0, w + 2f * x, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;

                RenderWireFrame._wireFrame = GUI.Toggle(new Rect(x, y, w, h), RenderWireFrame._wireFrame, "Wireframe"); y += h;

                OceanRenderer.Instance._freezeTime = GUI.Toggle(new Rect(x, y, w, h), OceanRenderer.Instance._freezeTime, "Freeze waves"); y += h;

                GUI.Label(new Rect(x, y, w, h), string.Format("Chop: {0}", OceanRenderer.Instance._chop.ToString("0.00"))); y += h;
                OceanRenderer.Instance._chop = GUI.HorizontalSlider(new Rect(x, y, w, h), OceanRenderer.Instance._chop, 0f, 2f); y += h;

                OceanRenderer.Instance._enableSmoothLOD = GUI.Toggle(new Rect(x, y, w, h), OceanRenderer.Instance._enableSmoothLOD, "Enable smooth LOD"); y += h;

                GUI.Label(new Rect(x, y, w, h), string.Format("Min verts per wave: {0}", OceanRenderer.Instance._minTexelsPerWave.ToString("0.00"))); y += h;
                OceanRenderer.Instance._minTexelsPerWave = GUI.HorizontalSlider(new Rect(x, y, w, h), OceanRenderer.Instance._minTexelsPerWave, 0, 15); y += h;

                _showSimTargets = GUI.Toggle(new Rect(x, y, w, h), _showSimTargets, "Show shape data"); y += h;
                WaveDataCam._shapeCombinePass = GUI.Toggle(new Rect(x, y, w, h), WaveDataCam._shapeCombinePass, "Shape combine pass"); y += h;

                OceanRenderer._acceptLargeWavelengthsInLastLOD = GUI.Toggle(new Rect(x, y, w, h), OceanRenderer._acceptLargeWavelengthsInLastLOD, "Large waves in last LOD"); y += h;

                if (GUI.Button(new Rect(x, y, w, h), "Hide GUI (G)"))
                {
                    ToggleGUI();
                }
                y += h;
            }

            // draw source textures to screen
            if ( _showSimTargets )
            {
                DrawShapeTargets();
            }

            GUI.color = bkp;
        }

        void DrawShapeTargets()
        {
            int ind = 0;
            foreach (var cam in OceanRenderer.Instance.Builder._shapeCameras)
            {
                if (!cam) continue;

                RenderTexture shape = cam.targetTexture;

                if (shape == null) continue;

                float b = 7f;
                float h = Screen.height / (float)OceanRenderer.Instance.Builder._shapeCameras.Length;
                float w = h + b;
                float x = Screen.width - w;
                float y = ind * h;

                GUI.color = Color.black * 0.7f;
                GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(x + b, y + b / 2f, h - b, h - b), shape, ScaleMode.ScaleAndCrop, false);

                ind++;
            }
        }

        void ToggleGUI()
        {
            _guiVisible = !_guiVisible;
        }
    }
}
