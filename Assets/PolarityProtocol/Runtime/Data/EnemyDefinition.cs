using UnityEngine;

namespace PolarityProtocol.Data
{
    public enum EnemyArchetype
    {
        Chaser,
        Shooter,
        Shield
    }

    [CreateAssetMenu(menuName = "Polarity Protocol/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private EnemyArchetype archetype;
        [SerializeField, Min(1f)] private float maximumHealth = 70f;
        [SerializeField, Min(0.5f)] private float movementSpeed = 5f;
        [SerializeField, Min(0f)] private float acceleration = 28f;
        [SerializeField, Min(0.5f)] private float attackRange = 2.2f;
        [SerializeField, Min(0f)] private float attackDamage = 18f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.8f;
        [SerializeField, Min(0.05f)] private float telegraphDuration = 0.55f;
        [SerializeField, Min(1f)] private float perceptionRange = 30f;
        [SerializeField, Min(0f)] private float preferredRange = 12f;
        [SerializeField, Min(0f)] private float projectileSpeed = 13f;
        [SerializeField] private Color accent = Color.red;

        public EnemyArchetype Archetype => archetype;
        public float MaximumHealth => maximumHealth;
        public float MovementSpeed => movementSpeed;
        public float Acceleration => acceleration;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public float TelegraphDuration => telegraphDuration;
        public float PerceptionRange => perceptionRange;
        public float PreferredRange => preferredRange;
        public float ProjectileSpeed => projectileSpeed;
        public Color Accent => accent;

        public void Configure(
            EnemyArchetype kind,
            float health,
            float speed,
            float range,
            float damage,
            float cooldown,
            Color color)
        {
            archetype = kind;
            maximumHealth = health;
            movementSpeed = speed;
            acceleration = kind == EnemyArchetype.Shooter ? 22f : 30f;
            attackRange = range;
            attackDamage = damage;
            attackCooldown = cooldown;
            telegraphDuration = kind == EnemyArchetype.Shooter ? 0.75f : 0.5f;
            perceptionRange = 35f;
            preferredRange = kind == EnemyArchetype.Shooter ? 12f : range * 0.75f;
            projectileSpeed = 14f;
            accent = color;
        }
    }
}

