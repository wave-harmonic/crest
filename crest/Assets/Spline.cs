using UnityEditor;
using UnityEngine;

namespace Crest.Spline
{
    public class Spline : MonoBehaviour
    {
        [SerializeField, Delayed]
        int _subdivisions = 0;

        [SerializeField]
        float _radius = 20f;

        [SerializeField, Delayed]
        int _smoothingIterations = 60;

        Mesh _mesh;

        SplinePoint[] SplinePoints => GetComponentsInChildren<SplinePoint>();

        Vector3 TangentAfter(SplinePoint[] splinePoints, int idx)
        {
            var tangent = Vector3.zero;
            var wt = 0f;
            //var idx = i / 3;
            if (idx - 1 >= 0)
            {
                tangent += splinePoints[idx].transform.position - splinePoints[idx - 1].transform.position;
                wt += 1f;
            }
            if (idx + 1 < splinePoints.Length)
            {
                tangent += splinePoints[idx + 1].transform.position - splinePoints[idx].transform.position;
                wt += 1f;
            }
            return tangent / wt;
        }

        Vector3 TangentBefore(SplinePoint[] splinePoints, int idx)
        {
            var tangent = Vector3.zero;
            var wt = 0f;
            if (idx - 1 >= 0)
            {
                tangent += splinePoints[idx].transform.position - splinePoints[idx - 1].transform.position;
                wt += 1f;
            }
            if (idx + 1 < splinePoints.Length)
            {
                tangent += splinePoints[idx + 1].transform.position - splinePoints[idx].transform.position;
                wt += 1f;
            }
            return tangent / wt;
        }

        void OnDrawGizmos()
        {
            var splinePoints = SplinePoints;
            if (splinePoints.Length < 2) return;

            Vector3[] points = new Vector3[(splinePoints.Length - 1) * 3 + 1];
            for (int i = 0; i < points.Length; i++)
            {
                float tm = 0.5f;

                if (i % 3 == 0)
                {
                    points[i] = splinePoints[i / 3].transform.position;
                }
                else if (i % 3 == 1)
                {
                    var idx = i / 3;
                    var tangent = TangentAfter(splinePoints, idx);
                    tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                    points[i] = splinePoints[idx].transform.position + tm * tangent;

                    if (i == 1)
                    {
                        tangent = TangentBefore(splinePoints, idx + 1);
                        // Mirror first tangent
                        var toNext = (splinePoints[idx + 1].transform.position - splinePoints[idx].transform.position).normalized;
                        var nearestPoint = Vector3.Dot(tangent, toNext) * toNext;
                        tangent += 2f * (nearestPoint - tangent);
                        tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                        points[i] = splinePoints[idx].transform.position + tm * tangent;
                    }
                }
                else
                {
                    var idx = i / 3 + 1;
                    var tangent = TangentBefore(splinePoints, idx);
                    tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                    points[i] = splinePoints[idx].transform.position - tm * tangent;

                    if (i == points.Length - 2)
                    {
                        tangent = TangentAfter(splinePoints, idx - 1);
                        // Mirror first tangent
                        var toNext = (splinePoints[idx - 1].transform.position - splinePoints[idx].transform.position).normalized;
                        var nearestPoint = Vector3.Dot(tangent, toNext) * toNext;
                        tangent += 2f * (nearestPoint - tangent);
                        tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                        points[i] = splinePoints[idx].transform.position - tm * tangent;
                    }
                }
            }

            if (splinePoints.Length > 1)
            {
                Handles.color = Color.red;

                float lengthEst = 0f;
                for (int i = 1; i < splinePoints.Length; i++)
                {
                    lengthEst += (splinePoints[i].transform.position - splinePoints[i - 1].transform.position).magnitude;
                }
                lengthEst = Mathf.Max(lengthEst, 1f);
                float spacing = 16f / Mathf.Pow(2f, _subdivisions + 2);
                int pointCount = Mathf.RoundToInt(lengthEst / spacing);

                var resultPts0 = new Vector3[pointCount];

                resultPts0[0] = points[0];
                for (int i = 1; i < pointCount; i++)
                {
                    float t = i / (float)(pointCount - 1);

                    var tpts = t * (splinePoints.Length - 1);
                    var spidx = Mathf.FloorToInt(tpts);
                    var alpha = tpts - spidx;
                    if (spidx == splinePoints.Length - 1)
                    {
                        spidx -= 1;
                        alpha = 1f;
                    }
                    var pidx = spidx * 3;

                    resultPts0[i] = (1 - alpha) * (1 - alpha) * (1 - alpha) * points[pidx] + 3 * alpha * (1 - alpha) * (1 - alpha) * points[pidx + 1] + 3 * alpha * alpha * (1 - alpha) * points[pidx + 2] + alpha * alpha * alpha * points[pidx + 3];

                    Handles.DrawLine(resultPts0[i], resultPts0[i - 1]);
                }

                var resultPts1 = new Vector3[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    var tangent = resultPts0[Mathf.Min(pointCount - 1, i + 1)] - resultPts0[Mathf.Max(0, i - 1)];
                    var normal = tangent;
                    normal.x = tangent.z;
                    normal.z = -tangent.x;
                    normal = normal.normalized;
                    resultPts1[i] = resultPts0[i] + normal * _radius;
                }

                var resultPtsTmp = new Vector3[pointCount];
                for (int j = 0; j < _smoothingIterations; j++)
                {
                    resultPtsTmp[0] = resultPts1[0];
                    resultPtsTmp[pointCount - 1] = resultPts1[pointCount - 1];
                    for (int i = 1; i < pointCount - 1; i++)
                    {
                        resultPtsTmp[i] = (resultPts1[i] + resultPts1[i + 1] + resultPts1[i - 1]) / 3f;
                        resultPtsTmp[i] = resultPts0[i] + (resultPtsTmp[i] - resultPts0[i]).normalized * _radius;
                    }
                    var tmp = resultPts1;
                    resultPts1 = resultPtsTmp;
                    resultPtsTmp = tmp;
                }

                //for (int i = 0; i < pointCount; i++)
                //{
                //    Handles.DrawLine(resultPts0[i], resultPts1[i]);
                //}

                Handles.color = Color.white;

                CreateMesh(resultPts0, resultPts1);
            }
        }

        void CreateMesh(Vector3[] resultPts0, Vector3[] resultPts1)
        {
            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = name + "_mesh";
            }

            var triCount = (resultPts0.Length - 1) * 2;
            var verts = new Vector3[triCount+2];
            var indices = new int[triCount * 6];
            for (var i = 0; i < resultPts0.Length - 1; i += 1)
            {
                verts[2 * i] = transform.InverseTransformPoint(resultPts0[i]);
                verts[2 * i + 1] = transform.InverseTransformPoint(resultPts1[i]);
                verts[2 * (i + 1)] = transform.InverseTransformPoint(resultPts0[(i + 1)]);
                verts[2 * (i + 1) + 1] = transform.InverseTransformPoint(resultPts1[(i + 1)]);
                indices[i * 6] = 2 * i;
                indices[i * 6 + 1] = 2 * (i + 1);
                indices[i * 6 + 2] = 2 * i + 1;

                indices[i * 6 + 3] = 2 * (i + 1);
                indices[i * 6 + 4] = 2 * (i + 1) + 1;
                indices[i * 6 + 5] = 2 * i + 1;
            }

            _mesh.SetIndices(new int[] {}, MeshTopology.Triangles, 0);
            _mesh.vertices = verts;
            _mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            var mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
            mf.mesh = _mesh;
        }
    }
}
