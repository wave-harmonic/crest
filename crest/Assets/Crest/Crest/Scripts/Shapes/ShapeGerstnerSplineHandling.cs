// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Generates mesh suitable for rendering Gerstner waves from a spline
    /// </summary>
    public static class ShapeGerstnerSplineHandling
    {
        public static bool GenerateMeshFromSpline(Spline.Spline spline, Transform transform, int subdivisions, float radius, Vector2 customDataDefault, ref Mesh mesh, out float minHeight, out float maxHeight)
        {
            return GenerateMeshFromSpline<SplinePointDataNone>(spline, transform, subdivisions, radius, customDataDefault, ref mesh, out minHeight, out maxHeight);
        }

        public static bool GenerateMeshFromSpline<SplinePointCustomData>(Spline.Spline spline, Transform transform, int subdivisions, float radius, Vector2 customDataDefault, ref Mesh mesh, out float minHeight, out float maxHeight)
            where SplinePointCustomData : ISplinePointCustomData
        {
            minHeight = 10000f;
            maxHeight = -10000f;

            var splinePoints = spline.GetComponentsInChildren<SplinePoint>();

            foreach (var sp in splinePoints)
            {
                minHeight = Mathf.Min(minHeight, sp.transform.position.y);
                maxHeight = Mathf.Max(maxHeight, sp.transform.position.y);
            }

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

            var sampledPtsOnSpline = new Vector3[pointCount];
            var sampledPtsOffSplineLeft = new Vector3[pointCount];
            var sampledPtsOffSplineRight = new Vector3[pointCount];
            var sampledPointsScratch = new Vector3[pointCount];

            // First set of sample points lie on spline
            sampledPtsOnSpline[0] = points[0];

            // Default spline data - applies to all geom generation / input types
            var radiusMultiplier = new float[pointCount];
            radiusMultiplier[0] = 1f;
            if (splinePoints[0].TryGetComponent(out SplinePointData splineDataComp00))
            {
                radiusMultiplier[0] = splineDataComp00.GetData().x;
            }

            // Custom spline data - specific to this construction
            var customData = new Vector2[pointCount];
            customData[0] = customDataDefault;
            if (splinePoints[0].TryGetComponent(out SplinePointCustomData customDataComp00))
            {
                customData[0] = customDataComp00.GetData();
            }

            for (var i = 1; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);

                SplineInterpolation.InterpolateCubicPosition(splinePointCount, points, t, out sampledPtsOnSpline[i]);

                var tpts = t * (splinePoints.Length - 1f);
                var spidx = Mathf.FloorToInt(tpts);
                var alpha = tpts - spidx;

                // Interpolate default data
                var splineData0 = 1f;
                if (splinePoints[spidx].TryGetComponent(out SplinePointData splineDataComp0))
                {
                    splineData0 = splineDataComp0.GetData().x;
                }
                var splineData1 = 1f;
                if (splinePoints[Mathf.Min(spidx + 1, splinePoints.Length - 1)].TryGetComponent(out SplinePointData splineDataComp1))
                {
                    splineData1 = splineDataComp1.GetData().x;
                }
                radiusMultiplier[i] = Mathf.Lerp(splineData0, splineData1, Mathf.SmoothStep(0f, 1f, alpha));

                // Interpolate custom data
                var customData0 = customDataDefault;
                if (splinePoints[spidx].TryGetComponent(out SplinePointCustomData customDataComp0))
                {
                    customData0 = customDataComp0.GetData();
                }
                var customData1 = customDataDefault;
                if (splinePoints[Mathf.Min(spidx + 1, splinePoints.Length - 1)].TryGetComponent(out SplinePointCustomData customDataComp1))
                {
                    customData1 = customDataComp1.GetData();
                }
                customData[i] = Vector2.Lerp(customData0, customData1, Mathf.SmoothStep(0f, 1f, alpha));
            }

            float radiusLeft, radiusRight;
            if (spline._offset == Spline.Spline.Offset.Left)
            {
                radiusLeft = radius;
                radiusRight = 0f;
            }
            else if (spline._offset == Spline.Spline.Offset.Center)
            {
                radiusLeft = radiusRight = 0.5f * radius;
            }
            else
            {
                radiusLeft = 0f;
                radiusRight = radius;
            }

            // Compute pairs of points to form the ribbon
            for (var i = 0; i < pointCount; i++)
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

                var tangent = sampledPtsOnSpline[iafter] - sampledPtsOnSpline[ibefore];
                var normal = tangent;
                normal.x = tangent.z;
                normal.z = -tangent.x;
                normal.y = 0f;
                normal = normal.normalized;
                sampledPtsOffSplineLeft[i] = sampledPtsOnSpline[i] - normal * radiusLeft * radiusMultiplier[i];
                sampledPtsOffSplineRight[i] = sampledPtsOnSpline[i] + normal * radiusRight * radiusMultiplier[i];
            }
            if (spline._closed)
            {
                var midPoint = Vector3.Lerp(sampledPtsOffSplineRight[0], sampledPtsOffSplineRight[sampledPtsOffSplineRight.Length - 1], 0.5f);
                sampledPtsOffSplineRight[0] = sampledPtsOffSplineRight[sampledPtsOffSplineRight.Length - 1] = midPoint;
            }

            // Fix cases where points reverse direction causing flipped triangles in result
            ResolveOverlaps(ref sampledPtsOffSplineLeft, sampledPtsOnSpline);
            ResolveOverlaps(ref sampledPtsOffSplineRight, sampledPtsOnSpline);

            // Do a few smoothing iterations just to try to soften results
            for (int j = 0; j < 5; j++)
            {
                for (int i = 1; i < sampledPtsOffSplineLeft.Length - 1; i++)
                {
                    sampledPointsScratch[i] = 0.5f * (sampledPtsOffSplineLeft[i - 1] + sampledPtsOffSplineLeft[i + 1]);
                }
                for (int i = 1; i < sampledPtsOffSplineLeft.Length - 1; i++)
                {
                    sampledPtsOffSplineLeft[i] = sampledPointsScratch[i];
                }

                for (int i = 1; i < sampledPtsOffSplineRight.Length - 1; i++)
                {
                    sampledPointsScratch[i] = 0.5f * (sampledPtsOffSplineRight[i - 1] + sampledPtsOffSplineRight[i + 1]);
                }
                for (int i = 1; i < sampledPtsOffSplineRight.Length - 1; i++)
                {
                    sampledPtsOffSplineRight[i] = sampledPointsScratch[i];
                }
            }

            return UpdateMesh(transform, sampledPtsOffSplineLeft, sampledPtsOffSplineRight, customData, spline._closed, ref mesh);
        }

        // Ensures that the set of points are always moving "forwards", where forwards direction is defined by
        // the spline points
        static void ResolveOverlaps(ref Vector3[] points, Vector3[] pointsOnSpline)
        {
            if (points.Length < 2)
            {
                return;
            }

            var pointsNew = new Vector3[points.Length];

            pointsNew[0] = points[0];

            // For each point after the first, check that it is "in front" of the last, compared
            // to the spline tangent
            var lastGoodPoint = points[1];
            for (int i = 1; i < points.Length; i++)
            {
                var tangentSpline = pointsOnSpline[i] - pointsOnSpline[i - 1];
                var tangent = points[i] - lastGoodPoint;

                // Do things flatland, weird cases can arise in full 3D
                tangent.y = tangentSpline.y = 0f;

                // Check if point has moved forward or not
                var dp = Vector3.Dot(tangent, tangentSpline);

                if (dp > 0f)
                {
                    // Forward movement, all good
                    pointsNew[i] = points[i];
                    lastGoodPoint = points[i];
                }
                else
                {
                    // Backpedal - use last good forward-moving point
                    pointsNew[i] = lastGoodPoint;

                    // But keep y value, to help avoid a bunch of invalid points collapsing to a single point
                    pointsNew[i].y = points[i].y;
                }
            }

            // Use resolved result
            points = pointsNew;
        }

        // Generates a mesh from the points sampled along the spline, and corresponding offset points. Bridges points with a ribbon of triangles.
        static bool UpdateMesh(Transform transform, Vector3[] sampledPtsOffSplineLeft, Vector3[] sampledPtsOffSplineRight, Vector2[] customData, bool closed, ref Mesh mesh)
        {
            if (mesh == null)
            {
                mesh = new Mesh();
            }

            // This shows the setup if spline offset is 'right' - ribbon extends out to right hand side of spline
            //                       \
            //               \   ___--4
            //                4--      \
            //                 \        \
            //  splinePoint1 -> 3--------3
            //                  |        |
            //                  2--------2
            //                  |        |
            //                  1--------1
            //                  |        |
            //  splinePoint0 -> 0--------0
            //
            //                  ^        ^
            // sampledPointsOnSpline   sampledPointsOffSpline
            //

            var triCount = (sampledPtsOffSplineLeft.Length - 1) * 2;
            var indices = new int[triCount * 3];
            var vertCount = 2 * sampledPtsOffSplineLeft.Length;
            var verts = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var uvs2 = new Vector2[vertCount];
            var uvs3 = new Vector2[vertCount];

            // This iterates over result points and emits a quad starting from the current result points (resultPts0[i0], resultPts1[i1]) to
            // the next result points. If the spline is closed, last quad bridges the last result points and the first result points.
            for (var i = 0; i < sampledPtsOffSplineLeft.Length; i += 1)
            {
                // Vert indices:
                //
                //              2i1------2i1+1
                //               |\       |
                //               |  \     |
                //               |    \   |
                //               |      \ |
                //              2i0------2i0+1
                //               |        |
                //               ~        ~
                //               |        |
                //    splinePoint0--------|
                //

                verts[2 * i] = transform.InverseTransformPoint(sampledPtsOffSplineLeft[i]);
                verts[2 * i + 1] = transform.InverseTransformPoint(sampledPtsOffSplineRight[i]);

                var axis0 = new Vector2(verts[2 * i].x - verts[2 * i + 1].x, verts[2 * i].z - verts[2 * i + 1].z).normalized;
                uvs[2 * i] = axis0;
                uvs[2 * i + 1] = axis0;

                // uvs2.x - 1-0 inverted normalized dist from shoreline
                uvs2[2 * i].x = 1f;
                uvs2[2 * i + 1].x = 0f;

                uvs3[2 * i] = customData[i];
                uvs3[2 * i + 1] = customData[i];

                // Emit two triangles
                if (i < sampledPtsOffSplineLeft.Length - 1)
                {
                    var inext = i + 1;

                    indices[i * 6] = 2 * i;
                    indices[i * 6 + 1] = 2 * inext;
                    indices[i * 6 + 2] = 2 * i + 1;

                    indices[i * 6 + 3] = 2 * inext;
                    indices[i * 6 + 4] = 2 * inext + 1;
                    indices[i * 6 + 5] = 2 * i + 1;
                }
            }

            mesh.SetIndices(new int[] { }, MeshTopology.Triangles, 0);
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.uv2 = uvs2;
            mesh.uv3 = uvs3;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();

            return true;
        }

        public static bool MinMaxHeightValid(float minHeight, float maxHeight) => maxHeight >= minHeight;
    }
}
