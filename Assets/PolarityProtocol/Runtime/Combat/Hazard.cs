using System.Collections.Generic;
using UnityEngine;

namespace PolarityProtocol.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class Hazard : MonoBehaviour
    {
        [SerializeField] private float damagePerSecond = 90f;
        private readonly Dictionary<Health, float> nextDamageTime = new();

        private void OnTriggerStay(Collider other)
        {
            Health health = other.GetComponentInParent<Health>();
            if (health == null || health.IsDead)
            {
                return;
            }

            if (nextDamageTime.TryGetValue(health, out float next) && Time.time < next)
            {
                return;
            }

            nextDamageTime[health] = Time.time + 0.2f;
            health.TakeDamage(new DamageInfo(
                damagePerSecond * 0.2f,
                gameObject,
                other.ClosestPoint(transform.position),
                Vector3.down,
                DamageType.Hazard));
        }

        private void OnTriggerExit(Collider other)
        {
            Health health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                nextDamageTime.Remove(health);
            }
        }
    }
}

