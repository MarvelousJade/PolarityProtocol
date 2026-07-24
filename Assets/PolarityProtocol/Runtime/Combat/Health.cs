using System;
using UnityEngine;

namespace PolarityProtocol.Combat
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximum = 100f;
        [SerializeField] private CombatFaction faction = CombatFaction.Neutral;

        private IDamageModifier[] damageModifiers = Array.Empty<IDamageModifier>();

        public event Action<DamageInfo, float> Damaged;
        public event Action<Health, DamageInfo> Died;

        public float Current { get; private set; }
        public float Maximum => maximum;
        public float Normalized => maximum <= 0f ? 0f : Current / maximum;
        public bool IsDead { get; private set; }
        public CombatFaction Faction => faction;

        private void Awake()
        {
            Current = maximum;
            CacheModifiers();
        }

        public void Configure(float maximumHealth, CombatFaction ownerFaction)
        {
            maximum = Mathf.Max(1f, maximumHealth);
            Current = maximum;
            faction = ownerFaction;
            IsDead = false;
            CacheModifiers();
        }

        public bool TakeDamage(DamageInfo damage)
        {
            if (IsDead || damage.Amount <= 0f)
            {
                return false;
            }

            for (int i = 0; i < damageModifiers.Length; i++)
            {
                damage = damageModifiers[i].ModifyDamage(damage);
            }

            float applied = Mathf.Clamp(damage.Amount, 0f, Current);
            if (applied <= 0f)
            {
                return false;
            }

            Current -= applied;
            Damaged?.Invoke(damage, applied);

            if (Current <= 0f)
            {
                Current = 0f;
                IsDead = true;
                Died?.Invoke(this, damage);
            }

            return true;
        }

        public void RestoreFull()
        {
            Current = maximum;
            IsDead = false;
        }

        private void CacheModifiers()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            int count = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageModifier)
                {
                    count++;
                }
            }

            damageModifiers = new IDamageModifier[count];
            int index = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageModifier modifier)
                {
                    damageModifiers[index++] = modifier;
                }
            }
        }
    }
}

