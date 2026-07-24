using UnityEngine;

namespace PolarityProtocol.Magnetics
{
    public static class MagneticForceSolver
    {
        public static Vector3 Calculate(
            Vector3 anchorPosition,
            Vector3 targetPosition,
            MagneticPolarity anchorPolarity,
            MagneticPolarity targetPolarity,
            float strength,
            float radius,
            float minimumDistance,
            float falloffExponent = 1.35f)
        {
            Vector3 anchorToTarget = targetPosition - anchorPosition;
            float actualDistance = anchorToTarget.magnitude;

            if (actualDistance > radius || strength <= 0f || radius <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 outward = actualDistance > 0.0001f ? anchorToTarget / actualDistance : Vector3.up;
            float safeDistance = Mathf.Max(actualDistance, Mathf.Max(0.01f, minimumDistance));
            float normalizedDistance = Mathf.Clamp01(safeDistance / radius);
            float falloff = Mathf.Pow(1f - normalizedDistance, Mathf.Max(0.1f, falloffExponent));
            bool attract = anchorPolarity != targetPolarity;

            return (attract ? -outward : outward) * (strength * falloff);
        }
    }
}

