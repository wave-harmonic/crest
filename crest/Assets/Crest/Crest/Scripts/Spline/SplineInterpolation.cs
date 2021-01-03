// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest.Spline
{
    public static class SplineInterpolation
    {
        /// <summary>
        /// Linearly interpolate between spline points
        /// </summary>
        /// <param name="points">The spline points</param>
        /// <param name="t">0-1 parameter along entire spline</param>
        /// <param name="position">Result position</param>
        public static void InterpolateLinearPosition(Vector3[] points, float t, out Vector3 position)
        {
            var tpts = t * (points.Length - 1f);
            var pidx = Mathf.FloorToInt(tpts);
            var alpha = tpts - pidx;
            if (pidx == points.Length - 1)
            {
                pidx -= 1;
                alpha = 1f;
            }

            position = Vector3.Lerp(points[pidx], points[pidx + 1], alpha);
        }

        /// <summary>
        /// Cubic interpolation of spline points
        /// </summary>
        /// <param name="splinePointCount">Number of user placed spline points (not including tangent points)</param>
        /// <param name="splinePointsAndTangents">The spline handle points and tangent points</param>
        /// <param name="t">0-1 parameter along entire spline</param>
        /// <param name="position">Result position</param>
        public static void InterpolateCubicPosition(float splinePointCount, Vector3[] splinePointsAndTangents, float t, out Vector3 position)
        {
            var tpts = t * (splinePointCount - 1f);
            var spidx = Mathf.FloorToInt(tpts);
            var alpha = tpts - spidx;
            if (spidx == splinePointCount - 1f)
            {
                spidx -= 1;
                alpha = 1f;
            }
            var pidx = spidx * 3;

            position =
                (1f - alpha) * (1f - alpha) * (1f - alpha) * splinePointsAndTangents[pidx]
                + 3 * alpha * (1f - alpha) * (1f - alpha) * splinePointsAndTangents[pidx + 1]
                + 3 * alpha * alpha * (1f - alpha) * splinePointsAndTangents[pidx + 2]
                + alpha * alpha * alpha * splinePointsAndTangents[pidx + 3];
        }

        /// <summary>
        /// Takes user-placed spline points and generates an array of points and generates an array of positions and tangents
        /// suitable for plugging into cubic interpolation
        /// </summary>
        /// <param name="splinePoints">Input user-placed spline positions</param>
        /// <param name="splinePointsAndTangents">Generated spline points and tangents</param>
        public static bool GenerateCubicSplineHull(SplinePoint[] splinePoints, Vector3[] splinePointsAndTangents)
        {
            if (splinePoints.Length < 2) return false;

            Debug.Assert(splinePointsAndTangents != null && splinePointsAndTangents.Length == (splinePoints.Length - 1) * 3 + 1,
                "splinePointsAndTangents array must be length {(splinePoints.Length - 1) * 3 + 1}");

            for (int i = 0; i < splinePointsAndTangents.Length; i++)
            {
                // Scale factor for tangents. Controls shape of resulting curve. When I placed spline points in a diamond, this
                // value produced a spline very close to a circle.
                float tm = 0.39f;

                if (i % 3 == 0)
                {
                    splinePointsAndTangents[i] = splinePoints[i / 3].transform.position;
                }
                else if (i % 3 == 1)
                {
                    var idx = i / 3;
                    var tangent = TangentAfter(splinePoints, idx);
                    tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                    splinePointsAndTangents[i] = splinePoints[idx].transform.position + tm * tangent;

                    if (i == 1)
                    {
                        tangent = TangentBefore(splinePoints, idx + 1);
                        // Mirror first tangent
                        var toNext = (splinePoints[idx + 1].transform.position - splinePoints[idx].transform.position).normalized;
                        var nearestPoint = Vector3.Dot(tangent, toNext) * toNext;
                        tangent += 2f * (nearestPoint - tangent);
                        tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                        splinePointsAndTangents[i] = splinePoints[idx].transform.position + tm * tangent;
                    }
                }
                else
                {
                    var idx = i / 3 + 1;
                    var tangent = TangentBefore(splinePoints, idx);
                    tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                    splinePointsAndTangents[i] = splinePoints[idx].transform.position - tm * tangent;

                    if (i == splinePointsAndTangents.Length - 2)
                    {
                        tangent = TangentAfter(splinePoints, idx - 1);
                        // Mirror first tangent
                        var toNext = (splinePoints[idx - 1].transform.position - splinePoints[idx].transform.position).normalized;
                        var nearestPoint = Vector3.Dot(tangent, toNext) * toNext;
                        tangent += 2f * (nearestPoint - tangent);
                        tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                        splinePointsAndTangents[i] = splinePoints[idx].transform.position - tm * tangent;
                    }
                }
            }

            return true;
        }

        static Vector3 TangentAfter(SplinePoint[] splinePoints, int idx)
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

        static Vector3 TangentBefore(SplinePoint[] splinePoints, int idx)
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
    }
}
