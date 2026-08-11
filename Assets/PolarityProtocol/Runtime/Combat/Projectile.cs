using PolarityProtocol.Magnetics;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Combat
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class Projectile : MonoBehaviour
    {
        private const float RedirectTurnRate = 7.5f;
        private const float RedirectAcceleration = 28f;
        private const float RedirectMinimumSpeed = 14f;
        private const float RedirectMaximumSpeed = 22f;

        private Rigidbody body;
        private Renderer visual;
        private Material visualMaterial;
        private TrailRenderer trail;
        private ProjectilePool pool;
        private GameObject owner;
        private CombatFaction faction;
        private float damage;
        private float lifetime;
        private bool released;
        private Color polarityColor;
        private MagneticTarget magneticTarget;
        private Transform redirectTarget;

        public CombatFaction Faction => faction;
        public bool WasRedirected { get; private set; }
        public MagneticPolarity Polarity => magneticTarget == null
            ? MagneticPolarity.Positive
            : magneticTarget.Polarity;
        public Vector3 Velocity => body == null ? Vector3.zero : body.linearVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            visual = GetComponent<Renderer>();
            visualMaterial = visual == null ? null : visual.material;
            trail = GetComponent<TrailRenderer>();
            magneticTarget = GetComponent<MagneticTarget>();
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Release();
            }
        }

        private void FixedUpdate()
        {
            if (!WasRedirected || redirectTarget == null || body == null)
            {
                return;
            }

            Vector3 targetPoint = redirectTarget.position + Vector3.up;
            Vector3 toTarget = targetPoint - transform.position;
            if (toTarget.sqrMagnitude <= 0.04f)
            {
                return;
            }

            Vector3 currentVelocity = body.linearVelocity;
            float speed = Mathf.Clamp(
                currentVelocity.magnitude,
                RedirectMinimumSpeed,
                RedirectMaximumSpeed);
            Vector3 steeredVelocity = Vector3.RotateTowards(
                currentVelocity.sqrMagnitude > 0.01f ? currentVelocity : transform.forward * speed,
                toTarget.normalized * speed,
                RedirectTurnRate * Time.fixedDeltaTime,
                RedirectAcceleration * Time.fixedDeltaTime);
            body.linearVelocity = Vector3.ClampMagnitude(steeredVelocity, RedirectMaximumSpeed);

            if (body.linearVelocity.sqrMagnitude > 0.01f)
            {
                transform.forward = body.linearVelocity.normalized;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (released || !gameObject.activeInHierarchy || other.isTrigger)
            {
                return;
            }

            Transform otherRoot = other.transform.root;
            if (owner != null && otherRoot.gameObject == owner.transform.root.gameObject)
            {
                return;
            }

            DamageReceiver receiver = other.GetComponentInParent<DamageReceiver>();
            if (receiver != null && receiver.Health != null)
            {
                if (receiver.Health.Faction == faction || receiver.Health.Faction == CombatFaction.Neutral)
                {
                    return;
                }

                Vector3 direction = body.linearVelocity.sqrMagnitude > 0.01f
                    ? body.linearVelocity.normalized
                    : transform.forward;
                receiver.Receive(new DamageInfo(
                    damage,
                    owner,
                    transform.position,
                    direction,
                    DamageType.Projectile));
                Release();
                return;
            }

            if (other.GetComponentInParent<MagneticAnchor>() == null)
            {
                Release();
            }
        }

        public void Launch(
            ProjectilePool owningPool,
            CombatFaction projectileFaction,
            GameObject projectileOwner,
            Vector3 position,
            Vector3 velocity,
            float projectileDamage,
            Color color,
            MagneticPolarity projectilePolarity = MagneticPolarity.Positive)
        {
            pool = owningPool;
            faction = projectileFaction;
            owner = projectileOwner;
            damage = projectileDamage;
            polarityColor = color;
            redirectTarget = null;
            magneticTarget?.Configure(projectilePolarity, 2.2f);
            lifetime = 8f;
            released = false;
            WasRedirected = false;
            transform.position = position;
            transform.forward = velocity.sqrMagnitude > 0.01f ? velocity.normalized : Vector3.forward;
            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;
            trail?.Clear();

            SetVisualColor(color, 2.5f);
        }

        public void RedirectByPlayer(GameObject anchorOwner)
        {
            if (faction != CombatFaction.Enemy)
            {
                return;
            }

            redirectTarget = owner == null ? null : owner.transform;
            faction = CombatFaction.Player;
            owner = anchorOwner;
            WasRedirected = true;
            damage *= 1.6f;
            lifetime = Mathf.Max(lifetime, 3f);

            // Ownership changes, but magnetic polarity does not. Keep the red/blue
            // polarity read intact and use stronger emission to confirm the redirect.
            SetVisualColor(polarityColor, 3.5f);
            Arena.GameSession.Active?.RegisterRedirect();
        }

        private void SetVisualColor(Color color, float emission)
        {
            if (visualMaterial == null)
            {
                return;
            }

            visualMaterial.color = color;
            if (visualMaterial.HasProperty("_EmissionColor"))
            {
                visualMaterial.EnableKeyword("_EMISSION");
                visualMaterial.SetColor("_EmissionColor", color * emission);
            }

            if (trail != null)
            {
                trail.startColor = new Color(color.r, color.g, color.b, 0.92f);
                trail.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }

        public void Release()
        {
            if (released)
            {
                return;
            }

            released = true;
            body.linearVelocity = Vector3.zero;
            trail?.Clear();
            pool?.Release(this);
        }
    }
}
