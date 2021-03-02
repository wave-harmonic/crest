// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the flow data. Attach this GameObjects that you want to influence the horizontal flow of the water volume.
    /// </summary>
    [ExecuteAlways]
    public class RegisterFlowInput : RegisterLodDataInputDisplacementCorrection<LodDataMgrFlow>
        , ISplinePointCustomDataSetup
#if UNITY_EDITOR
        , IReceiveSplinePointOnDrawGizmosSelectedMessages
#endif
    {
        public override bool Enabled => true;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(0f, 0f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Flow";

        [Header("Spline settings")]
        [SerializeField]
        float _radius = 20f;
        [SerializeField]
        int _subdivisions = 1;
        [SerializeField]
        int _smoothingIterations = 0;
        [SerializeField]
        float _speed = 5f;

        Material _splineFlowMaterial;

#if UNITY_EDITOR
        protected override bool RendererRequired => _spline == null;
        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateFlowSim;
        protected override string FeatureDisabledErrorMessage => "<i>Create Flow Sim</i> must be enabled on the OceanRenderer component to enable flow on the water surface.";
        protected override void FixOceanFeatureDisabled(SerializedObject oceanComponent)
        {
            oceanComponent.FindProperty("_createFlowSim").boolValue = true;
        }

        protected override string RequiredShaderKeyword => LodDataMgrFlow.MATERIAL_KEYWORD;
        protected override string KeywordMissingErrorMessage => LodDataMgrFlow.ERROR_MATERIAL_KEYWORD_MISSING;


        protected new void OnDrawGizmosSelected()
        {
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireMesh(_myMesh, transform.position, transform.rotation, transform.lossyScale);
        }

        public void OnSplinePointDrawGizmosSelected(SplinePoint point)
        {
            OnDrawGizmosSelected();
        }
#endif // UNITY_EDITOR

        Spline.Spline _spline;
        Mesh _myMesh;

        public override void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            if (weight <= 0f) return;

            if (_spline != null || TryGetComponent(out _spline))
            {
                if (_splineFlowMaterial == null)
                {
                    _splineFlowMaterial = new Material(Shader.Find("Hidden/Crest/Inputs/Flow/Spline Geometry"));
                }
                _splineFlowMaterial.SetFloat("_Speed", _speed);

                ShapeGerstnerSplineHandling.GenerateMeshFromSpline(_spline, transform, _subdivisions, _radius, _smoothingIterations, ref _myMesh);

                buf.SetGlobalFloat(sp_Weight, weight);
                buf.SetGlobalFloat(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                buf.SetGlobalVector(sp_DisplacementAtInputPosition, Vector3.zero);
                buf.DrawMesh(_myMesh, transform.localToWorldMatrix, _splineFlowMaterial);
            }
            else
            {
                base.Draw(buf, weight, isTransition, lodIdx);
            }
        }

        public bool AttachDataToSplinePoint(GameObject splinePoint)
        {
            if (splinePoint.TryGetComponent(out SplinePointDataFlow _))
            {
                // Already existing, nothing to do
                return false;
            }

            splinePoint.AddComponent<SplinePointDataFlow>();
            return true;
        }
    }
}
