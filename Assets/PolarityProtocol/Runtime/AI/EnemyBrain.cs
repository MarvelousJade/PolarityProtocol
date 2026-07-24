using System.Collections;
using PolarityProtocol.Arena;
using PolarityProtocol.Combat;
using PolarityProtocol.Data;
using PolarityProtocol.Player;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.AI
{
    public enum EnemyState
    {
        Acquiring,
        Pursuing,
        Repositioning,
        Telegraphing,
        Attacking,
        Recovering,
        Displaced,
        Dead
    }

    [RequireComponent(typeof(Rigidbody), typeof(Health))]
    public sealed class EnemyBrain : MonoBehaviour, IDamageModifier
    {
        private EnemyDefinition definition;
        private Transform target;
        private Rigidbody body;
        private Health health;
        private Renderer[] renderers;
        private Material[] bodyMaterials;
        private Color baseColor;
        private Transform shieldVisual;
        private Material shieldMaterial;
        private TextMesh debugLabel;
        private LineRenderer attackRangeRing;
        private LineRenderer perceptionRing;
        private float nextAttackTime;
        private float stateEndsAt;
        private float shieldExposedUntil;
        private float flashEndsAt;
        private Vector3 desiredMove;
        private bool configured;

        public static int ActiveCount { get; private set; }
        public EnemyState State { get; private set; } = EnemyState.Acquiring;
        public EnemyDefinition Definition => definition;
        public Health Health => health;
        public bool ShieldExposed => definition != null &&
                                     definition.Archetype == EnemyArchetype.Shield &&
                                     Time.time < shieldExposedUntil;

        private void Awake()
        {
            ActiveCount++;
            body = GetComponent<Rigidbody>();
            health = GetComponent<Health>();
        }

        private void OnDestroy()
        {
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            UpdatePresentation();
            if (health.IsDead || GameSession.Active == null || !GameSession.Active.IsRunning)
            {
                return;
            }

            if (target == null)
            {
                SetState(EnemyState.Acquiring);
                return;
            }

            float distance = Vector3.Distance(transform.position, target.position);

            if (State == EnemyState.Telegraphing)
            {
                desiredMove = Vector3.zero;
                if (Time.time >= stateEndsAt)
                {
                    PerformAttack(distance);
                }
                return;
            }

            if (State == EnemyState.Recovering)
            {
                desiredMove = Vector3.zero;
                if (Time.time >= stateEndsAt)
                {
                    SetState(EnemyState.Acquiring);
                }
                return;
            }

            if (definition.Archetype == EnemyArchetype.Shooter)
            {
                ThinkShooter(distance);
            }
            else
            {
                ThinkMelee(distance);
            }
        }

        private void FixedUpdate()
        {
            if (!configured || health.IsDead || GameSession.Active == null || !GameSession.Active.IsRunning)
            {
                return;
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 desiredVelocity = desiredMove * definition.MovementSpeed;
            Vector3 velocityDelta = Vector3.ClampMagnitude(
                desiredVelocity - planarVelocity,
                definition.Acceleration * Time.fixedDeltaTime);
            body.AddForce(velocityDelta, ForceMode.VelocityChange);

            if (planarVelocity.magnitude > definition.MovementSpeed * 1.6f)
            {
                Vector3 clamped = planarVelocity.normalized * definition.MovementSpeed * 1.6f;
                body.linearVelocity = new Vector3(clamped.x, body.linearVelocity.y, clamped.z);
            }

            if (target != null)
            {
                Vector3 look = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
                if (look.sqrMagnitude > 0.05f)
                {
                    Quaternion rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
                    body.MoveRotation(Quaternion.Slerp(body.rotation, rotation, Time.fixedDeltaTime * 7f));
                }
            }
        }

        public void Configure(
            EnemyDefinition enemyDefinition,
            Transform player,
            Renderer[] enemyRenderers,
            Transform shield,
            TextMesh label,
            LineRenderer attackRing,
            LineRenderer sightRing)
        {
            definition = enemyDefinition;
            target = player;
            renderers = enemyRenderers;
            bodyMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                bodyMaterials[i] = renderers[i] == null ? null : renderers[i].material;
            }
            shieldVisual = shield;
            shieldMaterial = shieldVisual == null ? null : shieldVisual.GetComponent<Renderer>().material;
            debugLabel = label;
            attackRangeRing = attackRing;
            perceptionRing = sightRing;
            baseColor = definition.Accent;
            health.Configure(definition.MaximumHealth, CombatFaction.Enemy);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            nextAttackTime = Time.time + 0.8f;
            configured = true;
            SetState(EnemyState.Acquiring);
        }

        public void NotifyMagneticForce(float magnitude)
        {
            if (!configured || magnitude < 8f)
            {
                return;
            }

            if (definition.Archetype == EnemyArchetype.Shield)
            {
                shieldExposedUntil = Mathf.Max(shieldExposedUntil, Time.time + 3.2f);
            }

            if (State != EnemyState.Dead && State != EnemyState.Telegraphing)
            {
                SetState(EnemyState.Displaced);
            }
        }

        public DamageInfo ModifyDamage(DamageInfo damage)
        {
            if (!configured ||
                definition.Archetype != EnemyArchetype.Shield ||
                ShieldExposed ||
                damage.Type == DamageType.Hazard)
            {
                return damage;
            }

            Vector3 targetToAttacker = damage.Direction.sqrMagnitude > 0.01f
                ? -damage.Direction.normalized
                : transform.forward;
            bool frontal = Vector3.Dot(transform.forward, targetToAttacker) > 0.25f;

            if (!frontal)
            {
                return damage;
            }

            FeedbackBus.Pulse(125f, 0.08f, 0.08f);
            CameraRig.Active?.AddTrauma(0.06f);
            return damage.WithAmount(damage.Amount * 0.08f);
        }

        private void ThinkMelee(float distance)
        {
            if (distance <= definition.AttackRange && Time.time >= nextAttackTime)
            {
                BeginTelegraph();
                return;
            }

            desiredMove = distance > definition.AttackRange * 0.8f
                ? DirectionToTarget()
                : Vector3.zero;
            SetState(desiredMove.sqrMagnitude > 0f ? EnemyState.Pursuing : EnemyState.Recovering, false);
        }

        private void ThinkShooter(float distance)
        {
            Vector3 direction = DirectionToTarget();
            if (distance < definition.PreferredRange * 0.65f)
            {
                desiredMove = -direction;
                SetState(EnemyState.Repositioning, false);
            }
            else if (distance > definition.PreferredRange * 1.25f)
            {
                desiredMove = direction;
                SetState(EnemyState.Pursuing, false);
            }
            else
            {
                float strafeSign = Mathf.Sin(Time.time * 0.8f + GetEntityId().GetHashCode()) >= 0f ? 1f : -1f;
                desiredMove = Vector3.Cross(Vector3.up, direction) * strafeSign * 0.45f;
                SetState(EnemyState.Repositioning, false);
            }

            if (distance <= definition.AttackRange && Time.time >= nextAttackTime)
            {
                BeginTelegraph();
            }
        }

        private void BeginTelegraph()
        {
            desiredMove = Vector3.zero;
            stateEndsAt = Time.time + definition.TelegraphDuration;
            SetState(EnemyState.Telegraphing);
            FeedbackBus.Pulse(
                definition.Archetype == EnemyArchetype.Shooter ? 410f : 160f,
                definition.TelegraphDuration * 0.45f,
                0.025f);
        }

        private void PerformAttack(float distance)
        {
            SetState(EnemyState.Attacking);

            if (definition.Archetype == EnemyArchetype.Shooter)
            {
                Vector3 origin = transform.position + Vector3.up * 1.3f + transform.forward * 0.8f;
                Vector3 aim = (target.position + Vector3.up - origin).normalized;
                ProjectilePool.Active?.Spawn(
                    CombatFaction.Enemy,
                    gameObject,
                    origin,
                    aim * definition.ProjectileSpeed,
                    definition.AttackDamage,
                    definition.Accent);
                CameraRig.Active?.AddTrauma(0.045f);
            }
            else if (distance <= definition.AttackRange + 0.85f)
            {
                DamageReceiver receiver = target.GetComponentInParent<DamageReceiver>();
                receiver?.Receive(new DamageInfo(
                    definition.AttackDamage,
                    gameObject,
                    target.position,
                    transform.forward,
                    DamageType.Kinetic));
                body.AddForce(-transform.forward * 2.5f, ForceMode.VelocityChange);
                CameraRig.Active?.AddTrauma(0.18f);
            }

            nextAttackTime = Time.time + definition.AttackCooldown;
            stateEndsAt = Time.time + 0.32f;
            SetState(EnemyState.Recovering);
        }

        private Vector3 DirectionToTarget()
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            return Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up).normalized;
        }

        private void SetState(EnemyState next, bool forcePresentation = true)
        {
            if (State == next && !forcePresentation)
            {
                return;
            }

            State = next;
        }

        private void UpdatePresentation()
        {
            Color displayColor = baseColor;
            if (Time.time < flashEndsAt)
            {
                displayColor = Color.white;
            }
            else if (State == EnemyState.Telegraphing)
            {
                float pulse = (Mathf.Sin(Time.time * 22f) + 1f) * 0.5f;
                displayColor = Color.Lerp(baseColor, Color.white, pulse);
            }

            for (int i = 0; i < bodyMaterials.Length; i++)
            {
                if (bodyMaterials[i] != null)
                {
                    bodyMaterials[i].color = displayColor;
                    if (bodyMaterials[i].HasProperty("_EmissionColor"))
                    {
                        bodyMaterials[i].EnableKeyword("_EMISSION");
                        bodyMaterials[i].SetColor("_EmissionColor", displayColor * 0.55f);
                    }
                }
            }

            if (shieldVisual != null && shieldMaterial != null)
            {
                Color shieldColor = ShieldExposed ? new Color(0.28f, 0.32f, 0.35f) : RuntimeArt.Gold;
                shieldMaterial.color = shieldColor;
                if (shieldMaterial.HasProperty("_EmissionColor"))
                {
                    shieldMaterial.EnableKeyword("_EMISSION");
                    shieldMaterial.SetColor("_EmissionColor", shieldColor * (ShieldExposed ? 0.1f : 1.2f));
                }
                shieldVisual.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    ShieldExposed ? 75f + Mathf.Sin(Time.time * 5f) * 8f : 0f);
            }

            bool showDebug = DebugOverlay.Enabled;
            if (debugLabel != null)
            {
                debugLabel.gameObject.SetActive(showDebug);
                if (showDebug)
                {
                    debugLabel.text =
                        $"{definition.Archetype.ToString().ToUpperInvariant()}\n{State}  {health.Current:0}/{health.Maximum:0}";
                    if (Camera.main != null)
                    {
                        debugLabel.transform.rotation = Camera.main.transform.rotation;
                    }
                }
            }

            if (attackRangeRing != null)
            {
                attackRangeRing.gameObject.SetActive(showDebug);
            }

            if (perceptionRing != null)
            {
                perceptionRing.gameObject.SetActive(showDebug);
            }
        }

        private void OnDamaged(DamageInfo damage, float applied)
        {
            flashEndsAt = Time.time + 0.08f;
            FeedbackBus.Pulse(210f, 0.045f, 0.05f);
            CameraRig.Active?.AddTrauma(Mathf.Clamp(applied / 200f, 0.03f, 0.14f));

            if (applied >= 20f)
            {
                FeedbackBus.HitStop(0.035f);
            }
        }

        private void OnDied(Health _, DamageInfo damage)
        {
            SetState(EnemyState.Dead);
            desiredMove = Vector3.zero;
            FeedbackBus.Pulse(75f, 0.18f, 0.14f);
            CameraRig.Active?.AddTrauma(0.25f);
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;
            while (elapsed < 0.7f)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / 0.7f);
                transform.localScale = originalScale * (1f - progress * 0.8f);
                transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
