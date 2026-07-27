using UnityEngine;

namespace PolarityProtocol.Data
{
    [CreateAssetMenu(menuName = "Polarity Protocol/Ability Definition", fileName = "MagneticAnchorAbility")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        [Header("Placement")]
        [SerializeField, Min(1f)] private float placementRange = 22f;
        [SerializeField, Range(1, 4)] private int maximumActiveAnchors = 2;

        [Header("Field")]
        [SerializeField, Min(1f)] private float radius = 7.5f;
        [SerializeField, Min(1f)] private float strength = 78f;
        [SerializeField, Min(0.05f)] private float minimumDistance = 0.8f;
        [SerializeField, Min(0.2f)] private float duration = 10f;
        [SerializeField, Min(0.1f)] private float falloffExponent = 1.1f;

        [Header("Resource")]
        [SerializeField, Min(0f)] private float cooldown = 0.7f;
        [SerializeField, Min(0f)] private float energyCost = 34f;
        [SerializeField, Min(1f)] private float maximumEnergy = 100f;
        [SerializeField, Min(0f)] private float energyRegeneration = 24f;

        public float PlacementRange => placementRange;
        public int MaximumActiveAnchors => maximumActiveAnchors;
        public float Radius => radius;
        public float Strength => strength;
        public float MinimumDistance => minimumDistance;
        public float Duration => duration;
        public float FalloffExponent => falloffExponent;
        public float Cooldown => cooldown;
        public float EnergyCost => energyCost;
        public float MaximumEnergy => maximumEnergy;
        public float EnergyRegeneration => energyRegeneration;

        public void ConfigureDemoDefaults()
        {
            placementRange = 22f;
            maximumActiveAnchors = 2;
            radius = 7.5f;
            strength = 78f;
            minimumDistance = 0.8f;
            duration = 10f;
            falloffExponent = 1.1f;
            cooldown = 0.7f;
            energyCost = 34f;
            maximumEnergy = 100f;
            energyRegeneration = 24f;
        }

        private void OnValidate()
        {
            placementRange = Mathf.Max(1f, placementRange);
            maximumActiveAnchors = Mathf.Clamp(maximumActiveAnchors, 1, 4);
            radius = Mathf.Max(1f, radius);
            strength = Mathf.Max(1f, strength);
            minimumDistance = Mathf.Clamp(minimumDistance, 0.05f, radius);
            duration = Mathf.Max(0.2f, duration);
            falloffExponent = Mathf.Max(0.1f, falloffExponent);
            cooldown = Mathf.Max(0f, cooldown);
            maximumEnergy = Mathf.Max(1f, maximumEnergy);
            energyCost = Mathf.Clamp(energyCost, 0f, maximumEnergy);
            energyRegeneration = Mathf.Max(0f, energyRegeneration);
        }
    }
}

