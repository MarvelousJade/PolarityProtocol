using PolarityProtocol.Magnetics;
using PolarityProtocol.Pooling;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Combat
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        private ComponentPool<Projectile> pool;

        public static ProjectilePool Active { get; private set; }
        public int ActiveCount { get; private set; }
        public int AvailableCount => pool?.AvailableCount ?? 0;
        public int TotalCreated => pool?.TotalCreated ?? 0;

        private void Awake()
        {
            Active = this;
            pool = new ComponentPool<Projectile>(CreateProjectile);
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        public Projectile Spawn(
            CombatFaction faction,
            GameObject owner,
            Vector3 position,
            Vector3 velocity,
            float damage,
            Color color)
        {
            Projectile projectile = pool.Get();
            ActiveCount++;
            projectile.Launch(this, faction, owner, position, velocity, damage, color);
            return projectile;
        }

        public void Release(Projectile projectile)
        {
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
            pool.Release(projectile);
        }

        private Projectile CreateProjectile()
        {
            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "Pooled Projectile (New)";
            projectileObject.transform.SetParent(transform);
            projectileObject.transform.localScale = Vector3.one * 0.38f;

            SphereCollider collider = projectileObject.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            Rigidbody body = projectileObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = 0.35f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            TrailRenderer trail = projectileObject.AddComponent<TrailRenderer>();
            trail.time = 0.32f;
            trail.minVertexDistance = 0.04f;
            trail.startWidth = 0.28f;
            trail.endWidth = 0.015f;
            trail.numCornerVertices = 3;
            trail.numCapVertices = 3;
            trail.alignment = LineAlignment.View;
            trail.sharedMaterial = RuntimeArt.Material(Color.white, 1f, true);

            MagneticTarget target = projectileObject.AddComponent<MagneticTarget>();
            target.Configure(MagneticPolarity.Positive, 2.2f);

            Projectile projectile = projectileObject.AddComponent<Projectile>();
            return projectile;
        }
    }
}
