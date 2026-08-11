using System.Collections.Generic;
using UnityEngine;

namespace PolarityProtocol.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class Hazard : MonoBehaviour
    {
        // The enemy capsule has a 0.55 m radius. Keeping its centre this far
        // outside the trigger prevents an apparently safe robot from overlapping
        // the plasma with the edge of its collider.
        private const float EnemyClearance = 0.8f;
        private const float AvoidDistance = 2.5f;
        private const float AvoidWeight = 2.8f;
        private const float AvoidTurnWeight = 1.15f;

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
        /// Routes a desired move direction around each plasma trigger. The bounds
        /// are expanded by the enemy collider radius, so avoiding with the robot's
        /// centre also keeps its body out of the damaging area.
        /// </summary>
        public static Vector3 SteerAway(Vector3 position, Vector3 desiredMove, float turnBias = 1f)
        {
            Vector3 steeredMove = Vector3.ProjectOnPlane(desiredMove, Vector3.up);

            for (int i = 0; i < Active.Count; i++)
            {
                Collider activeZone = Active[i].zone;
                if (activeZone == null || !activeZone.enabled)
                {
                    continue;
                }

                steeredMove = SteerAwayFromBounds(
                    position,
                    steeredMove,
                    activeZone.bounds,
                    turnBias);
            }

            return Vector3.ClampMagnitude(steeredMove, 1f);
        }

        /// <summary>
        /// Moves an authored spawn out of a plasma clearance area. This protects
        /// runtime-generated stress units and hand-authored encounters from taking
        /// hazard damage before their first AI step.
        /// </summary>
        public static Vector3 ResolveSafeSpawn(Vector3 position)
        {
            const float spawnGap = 0.05f;

            for (int i = 0; i < Active.Count; i++)
            {
                Collider activeZone = Active[i].zone;
                if (activeZone == null || !activeZone.enabled)
                {
                    continue;
                }

                position = ResolveSafeSpawn(position, activeZone.bounds, spawnGap);
            }

            return position;
        }

        public static Vector3 ResolveSafeSpawn(
            Vector3 position,
            Bounds bounds,
            float spawnGap = 0.05f)
        {
            float minX = bounds.min.x - EnemyClearance;
            float maxX = bounds.max.x + EnemyClearance;
            float minZ = bounds.min.z - EnemyClearance;
            float maxZ = bounds.max.z + EnemyClearance;
            if (position.x < minX || position.x > maxX ||
                position.z < minZ || position.z > maxZ)
            {
                return position;
            }

            Vector3 exit = NearestExitDirection(position, minX, maxX, minZ, maxZ);
            if (exit == Vector3.left)
            {
                position.x = minX - spawnGap;
            }
            else if (exit == Vector3.right)
            {
                position.x = maxX + spawnGap;
            }
            else if (exit == Vector3.back)
            {
                position.z = minZ - spawnGap;
            }
            else
            {
                position.z = maxZ + spawnGap;
            }

            return position;
        }

        public static Vector3 SteerAwayFromBounds(
            Vector3 position,
            Vector3 desiredMove,
            Bounds bounds,
            float turnBias = 1f)
        {
            float minX = bounds.min.x - EnemyClearance;
            float maxX = bounds.max.x + EnemyClearance;
            float minZ = bounds.min.z - EnemyClearance;
            float maxZ = bounds.max.z + EnemyClearance;

            bool insideX = position.x >= minX && position.x <= maxX;
            bool insideZ = position.z >= minZ && position.z <= maxZ;
            if (insideX && insideZ)
            {
                // A collision or a recently released magnet may leave an enemy in
                // the clearance area. Take the shortest route back to safe ground.
                return NearestExitDirection(position, minX, maxX, minZ, maxZ);
            }

            Vector3 closest = new(
                Mathf.Clamp(position.x, minX, maxX),
                position.y,
                Mathf.Clamp(position.z, minZ, maxZ));
            Vector3 away = position - closest;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance >= AvoidDistance || distance < 0.0001f)
            {
                return desiredMove;
            }

            Vector3 normal = away / distance;
            float inwardSpeed = -Vector3.Dot(desiredMove, normal);
            if (inwardSpeed <= 0f)
            {
                return desiredMove;
            }

            float proximity = 1f - distance / AvoidDistance;
            Vector3 tangent = new(-normal.z, 0f, normal.x);
            Vector3 centreOffset = position - bounds.center;
            float side = Vector3.Dot(centreOffset, tangent);
            if (Mathf.Abs(side) < 0.05f)
            {
                side = turnBias;
            }
            if (side < 0f)
            {
                tangent = -tangent;
            }

            // The normal keeps the capsule clear; the tangent prevents a robot
            // approaching head-on from stopping at the edge instead of routing on.
            return desiredMove +
                   normal * (proximity * AvoidWeight) +
                   tangent * (proximity * AvoidTurnWeight);
        }

        private static Vector3 NearestExitDirection(
            Vector3 position,
            float minX,
            float maxX,
            float minZ,
            float maxZ)
        {
            float nearest = position.x - minX;
            Vector3 direction = Vector3.left;

            float distance = maxX - position.x;
            if (distance < nearest)
            {
                nearest = distance;
                direction = Vector3.right;
            }

            distance = position.z - minZ;
            if (distance < nearest)
            {
                nearest = distance;
                direction = Vector3.back;
            }

            if (maxZ - position.z < nearest)
            {
                direction = Vector3.forward;
            }

            return direction;
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

