// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Generates mesh suitable for rendering gerstner waves from a spline
    /// </summary>
    public static class ShapeGerstnerSplineHandling
    {
        public static bool GenerateMeshFromSpline(Spline.Spline spline, Transform transform, int subdivisions, float radius, int smoothingIterations, ref Mesh mesh)
        {
            var splinePoints = spline.SplinePoints;
            if (splinePoints.Length < 2) return false;

            var splinePointCount = splinePoints.Length;
            if (spline._closed && splinePointCount > 2)
            {
                splinePointCount++;
            }

            var points = new Vector3[(splinePointCount - 1) * 3 + 1];

            if (!SplineInterpolation.GenerateCubicSplineHull(splinePoints, points, spline._closed))
            {
                return false;
            }

            // Sample spline

            // Estimate total length of spline and use this to compute a sample count
            var lengthEst = 0f;
            for (int i = 1; i < splinePointCount; i++)
            {
                lengthEst += (splinePoints[i % splinePoints.Length].transform.position - splinePoints[i - 1].transform.position).magnitude;
            }
            lengthEst = Mathf.Max(lengthEst, 1f);

            var spacing = 16f / Mathf.Pow(2f, subdivisions + 1);
            var pointCount = Mathf.CeilToInt(lengthEst / spacing);
            pointCount = Mathf.Max(pointCount, 1);

            var resultPts0 = new Vector3[pointCount];
            var resultPts1 = new Vector3[pointCount];

            // First set of sample points lie on spline
            resultPts0[0] = points[0];
            for (int i = 1; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);

                SplineInterpolation.InterpolateCubicPosition(splinePointCount, points, t, out resultPts0[i]);
            }

            // Second set of sample points lie off-spline - some distance to the right
            for (int i = 0; i < pointCount; i++)
            {
                var tangent = resultPts0[Mathf.Min(pointCount - 1, i + 1)] - resultPts0[Mathf.Max(0, i - 1)];
                var normal = tangent;
                normal.x = tangent.z;
                normal.z = -tangent.x;
                normal = normal.normalized;
                resultPts1[i] = resultPts0[i] + normal * radius;
            }

            // Blur the second set of points to help solve overlaps or large distortions. Not perfect but helps in many cases.
            if (smoothingIterations > 0)
            {
                var resultPtsTmp = new Vector3[pointCount];

                // Ring buffer stye access when closed spline
                if (!spline._closed)
                {
                    for (int j = 0; j < smoothingIterations; j++)
                    {
                        resultPtsTmp[0] = resultPts1[0];
                        resultPtsTmp[pointCount - 1] = resultPts1[pointCount - 1];
                        for (int i = 1; i < pointCount - 1; i++)
                        {
                            resultPtsTmp[i] = (resultPts1[i] + resultPts1[i + 1] + resultPts1[i - 1]) / 3f;
                            resultPtsTmp[i] = resultPts0[i] + (resultPtsTmp[i] - resultPts0[i]).normalized * radius;
                        }
                        var tmp = resultPts1;
                        resultPts1 = resultPtsTmp;
                        resultPtsTmp = tmp;
                    }
                }
                else
                {
                    for (int j = 0; j < smoothingIterations; j++)
                    {
                        for (int i = 0; i < pointCount; i++)
                        {
                            var ibefore = i - 1; if (ibefore < 0) ibefore += resultPts1.Length;
                            var iafter = i + 1; if (iafter >= pointCount) iafter -= resultPts1.Length;
                            resultPtsTmp[i] = (resultPts1[i] + resultPts1[iafter] + resultPts1[ibefore]) / 3f;
                            resultPtsTmp[i] = resultPts0[i] + (resultPtsTmp[i] - resultPts0[i]).normalized * radius;
                        }
                        var tmp = resultPts1;
                        resultPts1 = resultPtsTmp;
                        resultPtsTmp = tmp;
                    }
                }
            }

            return UpdateMesh(transform, resultPts0, resultPts1, spline._closed, ref mesh);
        }

        static bool UpdateMesh(Transform transform, Vector3[] resultPts0, Vector3[] resultPts1, bool closed, ref Mesh mesh)
        {
            if (mesh == null)
            {
                mesh = new Mesh();
            }

            var splineLength = 0f;
            for (int i = 1; i < resultPts0.Length; i++)
            {
                splineLength += (resultPts0[i] - resultPts0[i - 1]).magnitude;
            }

            //           \
            //   \   ___--4 uvs1 _-
            //    4--      \
            //     \        \
            //  sp1 3--------3
            //      |        |
            //      2--------2
            //      |        |
            //      1--------1
            //      |        |
            //  sp0 0--------0 uvs1 __
            //      ^        ^
            //     RP0s     RP1s
            //
            var triCount = (resultPts0.Length - 1) * 2;
            var verts = new Vector3[triCount + 2];
            var uvs = new Vector2[triCount + 2];
            var uvs2 = new Vector2[triCount + 2];
            var indices = new int[triCount * 6];
            var distSoFar = 0f;
            var emitCount = closed ? resultPts0.Length : (resultPts0.Length - 1);
            for (var i0 = 0; i0 < emitCount; i0 += 1)
            {
                // Vert indices:
                //
                //     2i1------2i1+1
                //      |\       |
                //      |  \     |
                //      |    \   |
                //      |      \ |
                //     2i0------2i0+1
                //      |        |
                //    sp0--------*
                //
                var i1 = (i0 + 1) % resultPts0.Length;

                verts[2 * i0] = transform.InverseTransformPoint(resultPts0[i0]);
                verts[2 * i0 + 1] = transform.InverseTransformPoint(resultPts1[i0]);
                verts[2 * i1] = transform.InverseTransformPoint(resultPts0[i1]);
                verts[2 * i1 + 1] = transform.InverseTransformPoint(resultPts1[i1]);

                var axis0 = -new Vector2(resultPts1[i0].x - resultPts0[i0].x, resultPts1[i0].z - resultPts0[i0].z).normalized;
                var axis1 = -new Vector2(resultPts1[i1].x - resultPts0[i1].x, resultPts1[i1].z - resultPts0[i1].z).normalized;
                uvs[2 * i0] = axis0;
                uvs[2 * i0 + 1] = axis0;
                uvs[2 * i1] = axis1;
                uvs[2 * i1 + 1] = axis1;

                // uvs2.x - Dist to closest spline end
                // uvs2.y - 1-0 inverted normalized dist from shoreline
                var nextDistSoFar = distSoFar + (resultPts0[i1] - resultPts0[i0]).magnitude;
                uvs2[2 * i0].x = uvs2[2 * i0 + 1].x = Mathf.Min(distSoFar, splineLength - distSoFar);
                uvs2[2 * i1].x = uvs2[2 * i1 + 1].x = Mathf.Min(nextDistSoFar, splineLength - nextDistSoFar);
                uvs2[2 * i0].y = uvs[2 * i1].y = 1f;
                uvs2[2 * i0 + 1].y = uvs[2 * i1 + 1].y = 0f;

                indices[i0 * 6] = 2 * i0;
                indices[i0 * 6 + 1] = 2 * i1;
                indices[i0 * 6 + 2] = 2 * i0 + 1;

                indices[i0 * 6 + 3] = 2 * i1;
                indices[i0 * 6 + 4] = 2 * i1 + 1;
                indices[i0 * 6 + 5] = 2 * i0 + 1;

                distSoFar = nextDistSoFar;
            }

            mesh.SetIndices(new int[] { }, MeshTopology.Triangles, 0);
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.uv2 = uvs2;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();

            return true;
        }
    }
}
