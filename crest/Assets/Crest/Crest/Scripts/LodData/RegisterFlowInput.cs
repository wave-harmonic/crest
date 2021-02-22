// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the flow data. Attach this GameObjects that you want to influence the horizontal flow of the water volume.
    /// </summary>
    [ExecuteAlways]
    public class RegisterFlowInput : RegisterLodDataInputDisplacementCorrection<LodDataMgrFlow>
    {
        public override bool Enabled => true;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(0f, 0f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Flow";

        Spline.Spline _spline;
        Mesh _myMesh;

        public Material _flowMaterial;

        public override void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            if (weight <= 0f) return;

            if (_flowMaterial != null && (_spline != null || TryGetComponent(out _spline)))
            {
                ShapeGerstnerSplineHandling.GenerateMeshFromSpline(_spline, transform, 2, 20f, 0, ref _myMesh);

                buf.SetGlobalFloat(sp_Weight, weight);
                buf.SetGlobalFloat(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                buf.SetGlobalVector(sp_DisplacementAtInputPosition, Vector3.zero);
                buf.DrawMesh(_myMesh, transform.localToWorldMatrix, _flowMaterial);
            }
            else
            {
                base.Draw(buf, weight, isTransition, lodIdx);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireMesh(_myMesh, transform.position, transform.rotation, transform.lossyScale);
        }
    }
}
