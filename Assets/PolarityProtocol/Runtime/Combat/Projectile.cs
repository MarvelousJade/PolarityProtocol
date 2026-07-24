using PolarityProtocol.Magnetics;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Combat
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class Projectile : MonoBehaviour
    {
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

        public CombatFaction Faction => faction;
        public bool WasRedirected { get; private set; }
        public Vector3 Velocity => body == null ? Vector3.zero : body.linearVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            visual = GetComponent<Renderer>();
            visualMaterial = visual == null ? null : visual.material;
            trail = GetComponent<TrailRenderer>();
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Release();
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
            Color color)
        {
            pool = owningPool;
            faction = projectileFaction;
            owner = projectileOwner;
            damage = projectileDamage;
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

            faction = CombatFaction.Player;
            owner = anchorOwner;
            WasRedirected = true;
            damage *= 1.6f;
            lifetime = Mathf.Max(lifetime, 3f);

            SetVisualColor(RuntimeArt.Pull, 3f);
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
