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
                var ibefore = i - 1;
                var iafter = i + 1;
                if (!spline._closed)
                {
                    // Not closed - clamp to range
                    ibefore = Mathf.Max(ibefore, 0);
                    iafter = Mathf.Min(iafter, pointCount - 1);
                }
                else
                {
                    // Closed - wrap into range
                    if (ibefore < 0) ibefore += pointCount;
                    iafter %= pointCount;
                }

                var tangent = resultPts0[iafter] - resultPts0[ibefore];
                var normal = tangent;
                normal.x = tangent.z;
                normal.z = -tangent.x;
                normal = normal.normalized;
                resultPts1[i] = resultPts0[i] + normal * radius;
            }
            if (spline._closed)
            {
                var midPoint = Vector3.Lerp(resultPts1[0], resultPts1[resultPts1.Length - 1], 0.5f);
                resultPts1[0] = resultPts1[resultPts1.Length - 1] = midPoint;
            }

            // Blur the second set of points to help solve overlaps or large distortions. Not perfect but helps in many cases.
            if (smoothingIterations > 0)
            {
                var resultPtsTmp = new Vector3[pointCount];

                // Ring buffer style access when closed spline
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
                        for (int i = 0; i < resultPts1.Length; i++)
                        {
                            // Slightly odd indexing. The first and last point are the same, the indices need to wrap to either the
                            // second element (if overflow) or the penultimate element (if underflow) to ensure tension is maintained at ends.
                            var ibefore = i - 1;
                            var iafter = i + 1;

                            if (ibefore < 0) ibefore = resultPts1.Length - 2;
                            if (iafter >= resultPts1.Length) iafter = 1;

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
            var indices = new int[triCount * 3];
            var vertCount = 2 * resultPts0.Length;
            var verts = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var uvs2 = new Vector2[vertCount];
            var distSoFar = 0f;
            // This iterates over result points and emits a quad starting from the current result points (resultPts0[i0], resultPts1[i1]) to
            // the next result points. If the spline is closed, last quad bridges the last result points and the first result points.
            for (var i0 = 0; i0 < resultPts0.Length - 1; i0 += 1)
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
                var i1 = i0 + 1;

                verts[2 * i0] = transform.InverseTransformPoint(resultPts0[i0]);
                verts[2 * i0 + 1] = transform.InverseTransformPoint(resultPts1[i0]);
                verts[2 * i1] = transform.InverseTransformPoint(resultPts0[i1]);
                verts[2 * i1 + 1] = transform.InverseTransformPoint(resultPts1[i1]);

                var axis0 = new Vector2(verts[2 * i0].x - verts[2 * i0 + 1].x, verts[2 * i0].z - verts[2 * i0 + 1].z).normalized;
                var axis1 = new Vector2(verts[2 * i1].x - verts[2 * i1 + 1].x, verts[2 * i1].z - verts[2 * i1 + 1].z).normalized;
                uvs[2 * i0] = axis0;
                uvs[2 * i0 + 1] = axis0;
                uvs[2 * i1] = axis1;
                uvs[2 * i1 + 1] = axis1;

                // uvs2.x - Dist to closest spline end
                // uvs2.y - 1-0 inverted normalized dist from shoreline
                var nextDistSoFar = distSoFar + (resultPts0[i1] - resultPts0[i0]).magnitude;
                uvs2[2 * i0].x = uvs2[2 * i0 + 1].x = Mathf.Min(distSoFar, splineLength - distSoFar);
                uvs2[2 * i1].x = uvs2[2 * i1 + 1].x = Mathf.Min(nextDistSoFar, splineLength - nextDistSoFar);
                uvs2[2 * i0].y = uvs2[2 * i1].y = 1f;
                uvs2[2 * i0 + 1].y = uvs2[2 * i1 + 1].y = 0f;

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
