using UnityEngine;

namespace PolarityProtocol.Combat
{
    public enum CombatFaction
    {
        Neutral,
        Player,
        Enemy
    }

    public enum DamageType
    {
        Kinetic,
        Projectile,
        Hazard
    }

    public readonly struct DamageInfo
    {
        public DamageInfo(
            float amount,
            GameObject source,
            Vector3 point,
            Vector3 direction,
            DamageType type)
        {
            Amount = amount;
            Source = source;
            Point = point;
            Direction = direction;
            Type = type;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public DamageType Type { get; }

        public DamageInfo WithAmount(float amount)
        {
            return new DamageInfo(amount, Source, Point, Direction, Type);
        }
    }

    public interface IDamageModifier
    {
        DamageInfo ModifyDamage(DamageInfo damage);
    }
}

