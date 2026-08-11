using System.Collections.Generic;
using PolarityProtocol.Arena;
using PolarityProtocol.Data;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Magnetics
{
    public sealed class MagneticAnchor : MonoBehaviour
    {
        private const int MaximumTargets = 64;
        private readonly Collider[] overlapResults = new Collider[MaximumTargets];
        private readonly HashSet<MagneticTarget> affectedThisStep = new();
        private readonly LineRenderer[] debugLines = new LineRenderer[20];

        private AbilityDefinition definition;
        private GameObject owner;
        private Transform core;
        private LineRenderer ring;
        private float remainingLifetime;
        private Color color;

        public MagneticPolarity Polarity { get; private set; }
        public float Radius => definition == null ? 0f : definition.Radius;
        public float RemainingLifetime => remainingLifetime;
        public GameObject Owner => owner;

        private void Update()
        {
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float pulse = 0.86f + Mathf.Sin(Time.time * 7f) * 0.12f;
            if (core != null)
            {
                core.localScale = Vector3.one * pulse;
                core.Rotate(Vector3.up, 90f * Time.deltaTime, Space.Self);
            }

            if (ring != null)
            {
                float alpha = 0.55f + Mathf.Sin(Time.time * 4f) * 0.15f;
                ring.startColor = new Color(color.r, color.g, color.b, alpha);
                ring.endColor = ring.startColor;
            }
        }

        private void FixedUpdate()
        {
            if (definition == null)
            {
                return;
            }

            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                definition.Radius,
                overlapResults,
                ~0,
                // Projectile hitboxes are triggers, so the field must include triggers
                // or hostile rounds never receive magnetic force.
                QueryTriggerInteraction.Collide);

            affectedThisStep.Clear();
            int lineIndex = 0;

            for (int i = 0; i < count; i++)
            {
                MagneticTarget target = overlapResults[i].GetComponentInParent<MagneticTarget>();
                if (target == null || !affectedThisStep.Add(target))
                {
                    continue;
                }

                Vector3 force = MagneticForceSolver.Calculate(
                    transform.position,
                    target.transform.position,
                    Polarity,
                    target.Polarity,
                    definition.Strength,
                    definition.Radius,
                    definition.MinimumDistance,
                    definition.FalloffExponent);

                target.ApplyMagneticForce(force, owner, Polarity);

                if (DebugOverlay.Enabled && lineIndex < debugLines.Length)
                {
                    LineRenderer line = GetDebugLine(lineIndex++);
                    line.gameObject.SetActive(true);
                    line.SetPosition(0, transform.position + Vector3.up * 0.25f);
                    line.SetPosition(1, target.transform.position);
                    float intensity = Mathf.Clamp01(force.magnitude / definition.Strength);
                    line.startColor = Color.Lerp(Color.white, color, intensity);
                    line.endColor = color;
                }
            }

            for (int i = lineIndex; i < debugLines.Length; i++)
            {
                if (debugLines[i] != null)
                {
                    debugLines[i].gameObject.SetActive(false);
                }
            }
        }

        public void Initialize(
            AbilityDefinition ability,
            MagneticPolarity polarity,
            GameObject anchorOwner)
        {
            definition = ability;
            Polarity = polarity;
            owner = anchorOwner;
            remainingLifetime = ability.Duration;
            color = polarity == MagneticPolarity.Negative ? RuntimeArt.Pull : RuntimeArt.Push;
            BuildVisual();
        }

        private void BuildVisual()
        {
            GameObject pedestal = RuntimeArt.Primitive(
                PrimitiveType.Cylinder,
                "Anchor Pedestal",
                transform,
                new Vector3(0f, 0.12f, 0f),
                new Vector3(0.6f, 0.12f, 0.6f),
                RuntimeArt.Dark,
                false);

            GameObject coreObject = RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Polarity Core",
                transform,
                new Vector3(0f, 0.8f, 0f),
                Vector3.one * 0.58f,
                color,
                false,
                2.8f);
            core = coreObject.transform;
            core.localEulerAngles = new Vector3(35f, 45f, 35f);

            GameObject top = RuntimeArt.Primitive(
                PrimitiveType.Cylinder,
                "Field Coil",
                transform,
                new Vector3(0f, 0.48f, 0f),
                new Vector3(0.7f, 0.04f, 0.7f),
                color,
                false,
                1.8f);
            ring = RuntimeArt.Ring(transform, definition.Radius, color, 0.07f, 80);
            _ = pedestal;
        }

        private LineRenderer GetDebugLine(int index)
        {
            if (debugLines[index] != null)
            {
                return debugLines[index];
            }

            GameObject lineObject = new($"Force {index + 1:00}");
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.035f;
            line.endWidth = 0.015f;
            line.sharedMaterial = RuntimeArt.Material(color, 2f, true);
            debugLines[index] = line;
            return line;
        }
    }
}
