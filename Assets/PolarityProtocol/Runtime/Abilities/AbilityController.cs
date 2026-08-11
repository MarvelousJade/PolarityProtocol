using System.Collections.Generic;
using PolarityProtocol.Arena;
using PolarityProtocol.Data;
using PolarityProtocol.Magnetics;
using PolarityProtocol.Player;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Abilities
{
    public sealed class AbilityController : MonoBehaviour
    {
        private readonly Queue<MagneticAnchor> activeAnchors = new();
        private AbilityDefinition definition;
        private Camera viewCamera;
        private GameObject placementPreview;
        private Renderer previewRenderer;
        private Material previewMaterial;
        private LineRenderer previewRing;
        private float cooldownRemaining;
        private bool placementValid;
        private Vector3 placementPoint;
        private Vector3 placementNormal = Vector3.up;

        public MagneticPolarity SelectedPolarity { get; private set; } = MagneticPolarity.Negative;
        public float Energy { get; private set; }
        public float MaximumEnergy => definition == null ? 100f : definition.MaximumEnergy;
        public float EnergyNormalized => MaximumEnergy <= 0f ? 0f : Energy / MaximumEnergy;
        public float CooldownRemaining => cooldownRemaining;
        public int ActiveAnchorCount
        {
            get
            {
                RemoveMissingAnchors();
                return activeAnchors.Count;
            }
        }

        private void Start()
        {
            if (definition == null)
            {
                definition = Resources.Load<AbilityDefinition>("Data/MagneticAnchorAbility");
            }

            viewCamera = Camera.main;
            Energy = definition == null ? 100f : definition.MaximumEnergy;
            BuildPlacementPreview();
        }

        private void Update()
        {
            if (definition == null)
            {
                return;
            }

            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
            Energy = Mathf.Min(
                definition.MaximumEnergy,
                Energy + definition.EnergyRegeneration * Time.deltaTime);

            bool acceptsInput = GameSession.Active != null && GameSession.Active.IsRunning;
            placementPreview.SetActive(acceptsInput);

            if (!acceptsInput)
            {
                return;
            }

            if (LegacyInput.TogglePolarityPressed)
            {
                SelectedPolarity = SelectedPolarity.Opposite();
                FeedbackBus.Pulse(SelectedPolarity == MagneticPolarity.Negative ? 520f : 240f, 0.07f, 0.08f);
            }

            if (LegacyInput.RecallPressed)
            {
                RecallAll();
            }

            UpdatePlacementPreview();

            if (LegacyInput.PlaceAnchorPressed)
            {
                TryPlaceAnchor();
            }
        }

        public void Configure(AbilityDefinition ability)
        {
            definition = ability;
            Energy = ability == null ? 100f : ability.MaximumEnergy;
        }

        public bool TryPlaceAnchor()
        {
            if (!placementValid || definition == null || cooldownRemaining > 0f || Energy < definition.EnergyCost)
            {
                FeedbackBus.Pulse(110f, 0.08f, 0.05f);
                return false;
            }

            RemoveMissingAnchors();
            while (activeAnchors.Count >= definition.MaximumActiveAnchors)
            {
                MagneticAnchor oldest = activeAnchors.Dequeue();
                if (oldest != null)
                {
                    Destroy(oldest.gameObject);
                }
            }

            GameObject anchorObject = new($"{SelectedPolarity.Label()} Anchor");
            anchorObject.transform.position = placementPoint + placementNormal * 0.05f;
            anchorObject.transform.up = placementNormal;
            MagneticAnchor anchor = anchorObject.AddComponent<MagneticAnchor>();
            anchor.Initialize(definition, SelectedPolarity, gameObject);
            activeAnchors.Enqueue(anchor);

            Energy -= definition.EnergyCost;
            cooldownRemaining = definition.Cooldown;
            FeedbackBus.Pulse(
                SelectedPolarity == MagneticPolarity.Negative ? 320f : 150f,
                0.12f,
                0.16f);
            CameraRig.Active?.AddTrauma(0.14f);
            return true;
        }

        public void RecallAll()
        {
            while (activeAnchors.Count > 0)
            {
                MagneticAnchor anchor = activeAnchors.Dequeue();
                if (anchor != null)
                {
                    Destroy(anchor.gameObject);
                }
            }

            Energy = Mathf.Min(definition.MaximumEnergy, Energy + definition.EnergyCost * 0.35f);
            FeedbackBus.Pulse(440f, 0.07f, 0.06f);
        }

        private void UpdatePlacementPreview()
        {
            viewCamera ??= Camera.main;
            placementValid = false;

            if (viewCamera != null)
            {
                Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit[] hits = Physics.RaycastAll(
                    ray,
                    definition.PlacementRange,
                    ~0,
                    QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider.transform.root == transform.root)
                    {
                        continue;
                    }

                    placementPoint = hits[i].point;
                    placementNormal = hits[i].normal;
                    placementValid = Vector3.Distance(transform.position, placementPoint) <= definition.PlacementRange;
                    break;
                }
            }

            if (!placementValid)
            {
                Vector3 forward = viewCamera == null ? transform.forward : viewCamera.transform.forward;
                Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
                placementPoint = transform.position + flatForward * Mathf.Min(10f, definition.PlacementRange);
                placementPoint.y = 0.02f;
                placementNormal = Vector3.up;
                placementValid = true;
            }

            placementPreview.transform.position = placementPoint + placementNormal * 0.06f;
            placementPreview.transform.up = placementNormal;

            Color polarityColor = SelectedPolarity == MagneticPolarity.Negative ? RuntimeArt.Pull : RuntimeArt.Push;
            Color stateColor = cooldownRemaining <= 0f && Energy >= definition.EnergyCost
                ? polarityColor
                : Color.gray;
            Color previewColor = new(stateColor.r, stateColor.g, stateColor.b, 0.32f);
            previewMaterial.color = previewColor;
            if (previewMaterial.HasProperty("_EmissionColor"))
            {
                previewMaterial.EnableKeyword("_EMISSION");
                previewMaterial.SetColor("_EmissionColor", stateColor * 1.2f);
            }
            Color ringColor = new(stateColor.r, stateColor.g, stateColor.b, 0.34f);
            previewRing.startColor = ringColor;
            previewRing.endColor = ringColor;
        }

        private void BuildPlacementPreview()
        {
            placementPreview = RuntimeArt.Primitive(
                PrimitiveType.Cylinder,
                "Anchor Placement Preview",
                null,
                Vector3.zero,
                new Vector3(0.45f, 0.03f, 0.45f),
                RuntimeArt.Pull,
                false,
                1f);
            previewRenderer = placementPreview.GetComponent<Renderer>();
            previewMaterial = previewRenderer.material;
            previewRing = RuntimeArt.Ring(
                placementPreview.transform,
                definition == null ? 7.5f : definition.Radius,
                RuntimeArt.Pull,
                0.022f,
                64);
            placementPreview.SetActive(false);
        }

        private void RemoveMissingAnchors()
        {
            if (activeAnchors.Count == 0)
            {
                return;
            }

            int count = activeAnchors.Count;
            for (int i = 0; i < count; i++)
            {
                MagneticAnchor anchor = activeAnchors.Dequeue();
                if (anchor != null)
                {
                    activeAnchors.Enqueue(anchor);
                }
            }
        }
    }
}
