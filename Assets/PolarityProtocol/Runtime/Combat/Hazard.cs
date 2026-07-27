using System.Collections.Generic;
using UnityEngine;

namespace PolarityProtocol.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class Hazard : MonoBehaviour
    {
        private const float AvoidMargin = 1.4f;
        private const float AvoidWeight = 2.4f;

        [SerializeField] private float damagePerSecond = 90f;
        private readonly Dictionary<Health, float> nextDamageTime = new();
        private static readonly List<Hazard> Active = new();
        private Collider zone;

        private void OnEnable()
        {
            zone = GetComponent<Collider>();
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        /// <summary>
        /// Bends a desired move direction away from any hazard the position is
        /// standing near. Callers skip this while a magnet is dragging the mover,
        /// which is what makes pushing enemies into plasma still work.
        /// </summary>
        public static Vector3 SteerAway(Vector3 position, Vector3 desiredMove)
        {
            Vector3 escape = Vector3.zero;

            for (int i = 0; i < Active.Count; i++)
            {
                Bounds bounds = Active[i].zone.bounds;
                Vector3 away = new(position.x - bounds.center.x, 0f, position.z - bounds.center.z);
                float radius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude + AvoidMargin;
                float distance = away.magnitude;

                if (distance > radius)
                {
                    continue;
                }

                Vector3 direction = distance > 0.01f ? away / distance : Vector3.forward;
                escape += direction * (1f - distance / radius);
            }

            if (escape.sqrMagnitude < 0.0001f)
            {
                return desiredMove;
            }

            return Vector3.ClampMagnitude(desiredMove + escape * AvoidWeight, 1f);
        }

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

