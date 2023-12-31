using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryFramework
{
    [System.Serializable]
    public class CubicBezierPath : IPath
    {
        public Vector3 start, end;
        public Vector3 startDir, endDir;
        public Vector3 controlPointA, controlPointB;

        private float approxLength = 0;
        public int segmentCountForApproximation; //?
        private float[] LUT; //input 0-1 linear, output 0-1 pathspace

        private bool _isValid = true;
        public bool IsValid => _isValid;
        public CubicBezierPath(Vector3 s, Vector3 e, Vector3 sDir, Vector3 eDir, float tRadius)
        {
            start = s;
            end = e;
            startDir = sDir;
            endDir = eDir;
            segmentCountForApproximation = Mathf.RoundToInt(Vector3.Distance(end, start) * 10);
            Solve(tRadius);
            _isValid = start != end;
        }
        public void Solve(float turnRadius)
        {
            controlPointA = start + startDir * turnRadius;
            controlPointB = end + endDir * turnRadius * -1;

            CalculateDistanceAndGenerateLUT();
        }
        public void CalculateDistanceAndGenerateLUT()
        {
            LUT = new float[segmentCountForApproximation];

            float pathDistPerSegment = 1f / segmentCountForApproximation;
            Vector3 prevPoint = start;
            Vector3 curPoint;
            float dist = 0;
            for (int i = 0; i < segmentCountForApproximation; i++)
            {
                curPoint = EvaluateCurve(i * pathDistPerSegment);
                dist += Vector3.Distance(prevPoint, curPoint);
                LUT[i] = dist;
                prevPoint = curPoint;
            }

            approxLength = dist;
        }
        public Vector3 GetWorldPointFromPathSpace(float pathPercent)
        {
            return EvaluateCurve(DistToT(pathPercent * approxLength));
        }
        //thank you freya
        public float DistToT(float dist)
        {
            if (dist > approxLength)
                return 1;
            if (dist < 0)
                return 0;

            for (int i = 0; i < LUT.Length; i++)
            {
                if (dist > LUT[i] && dist < LUT[i + 1])
                {
                    return Remap(dist, LUT[i], LUT[i + 1], (i / (LUT.Length - 1f)), (i + 1) / (LUT.Length - 1f));
                }
            }
            return dist / approxLength;
        }
        public Vector3 EvaluateCurve(float t)
        {
            //(1 - t)^3 * P0 + 3t(1-t)^2 * P1 + 3t^2 (1-t) * P2 + t^3 * P3
            return (Mathf.Pow(1 - t, 3) * start) +
                (3 * t * Mathf.Pow(1 - t, 2)) * controlPointA +
                (3 * t * t * (1 - t)) * controlPointB +
                t * t * t * end;
        }
        public (Vector3, float) GetClosestPoint(Vector3 worldPoint)
        {
            //Technically we should be running the closestpoint on line segment for each of the approximation segments,
            //but that sounds like it would be more expensives
            Vector3 minPoint = start;
            float minPathPoint = 0;
            float minDist = float.MaxValue;
            float pathDistPerSegment = 1f / segmentCountForApproximation;
            for (int i = 0; i < segmentCountForApproximation; i++)
            {
                Vector3 pp = GetWorldPointFromPathSpace(i * pathDistPerSegment);
                float dist = Vector3.Distance(worldPoint, pp);
                if (dist < minDist)
                {
                    minDist = dist;
                    minPoint = pp;
                    minPathPoint = i * pathDistPerSegment;
                }
            }
            return (minPoint, minPathPoint);
        }

        public Vector3 GetDirectionAtPoint(float t)
        {
            //-3(1-t)^2 * P0 + 3(1-t)^2 * P1 - 6t(1-t) * P1 - 3t^2 * P2 + 6t(1-t) * P2 + 3t^2 * P3
            Vector3 ret = (-3 * (1 - t) * (1 - t)) * start +
                (3 * (1 - t) * (1 - t)) * controlPointA -
                6 * t * (1 - t) * controlPointA -
                3 * t * t * controlPointB +
                6 * t * (1 - t) * controlPointB +
                3 * t * t * end;

            return ret.normalized;
        }

        public Vector3 GetStart() => start;
        public Vector3 GetEnd() => end;
        public float GetTotalLength() => approxLength;

        public (Vector3, Vector3, Vector3) GetPathVectors(float pathPercent)
        {
            return (GetDirectionAtPoint(pathPercent), GetRightAtPoint(pathPercent), GetUpAtPoint(pathPercent));
        }
        public Vector3 GetRightAtPoint(float pathPercent)
        {
            return Vector3.Cross(Vector3.up, GetDirectionAtPoint(pathPercent)).normalized;
        }
        public Quaternion GetRotationAtPoint(float pathPercent)
        {
            return Quaternion.LookRotation(GetDirectionAtPoint(pathPercent));
        }
        public Vector3 GetUpAtPoint(float pathPercent)
        {
            return Vector3.Cross(GetDirectionAtPoint(pathPercent), GetRightAtPoint(pathPercent)).normalized;
        }
        public static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

    }
}
