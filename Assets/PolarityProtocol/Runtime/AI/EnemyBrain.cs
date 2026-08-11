using System.Collections;
using PolarityProtocol.Arena;
using PolarityProtocol.Combat;
using PolarityProtocol.Data;
using PolarityProtocol.Magnetics;
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
    public sealed class EnemyBrain : MonoBehaviour, IDamageModifier, IHazardDamageGate
    {
        private EnemyDefinition definition;
        private Transform target;
        private Rigidbody body;
        private Health health;
        private Renderer[] renderers;
        private Material[] bodyMaterials;
        private Color baseColor;
        private Transform modelRoot;
        private Vector3 modelBasePosition;
        private Transform shieldVisual;
        private Material shieldMaterial;
        private Transform healthBarRoot;
        private Transform healthFill;
        private LineRenderer aimLine;
        private TextMesh debugLabel;
        private LineRenderer attackRangeRing;
        private LineRenderer perceptionRing;
        private float nextAttackTime;
        private float stateEndsAt;
        private float shieldExposedUntil;
        private float magnetHeldUntil;
        private float hazardVulnerableUntil;
        private float hazardTurnBias;
        private float flashEndsAt;
        private Vector3 desiredMove;
        private bool configured;
        private bool plateTornOff;
        private MagneticPolarity platePolarity = MagneticPolarity.Positive;

        public static int ActiveCount { get; private set; }
        public EnemyState State { get; private set; } = EnemyState.Acquiring;
        public EnemyDefinition Definition => definition;
        public Health Health => health;
        public MagneticPolarity PlatePolarity => platePolarity;
        public bool CanTakeHazardDamage => Time.time < hazardVulnerableUntil;
        public bool ShieldExposed => definition != null &&
                                     definition.Archetype == EnemyArchetype.Shield &&
                                     (plateTornOff || Time.time < shieldExposedUntil);

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
            bool magneticallyDisplaced = Time.time < magnetHeldUntil;

            // Normal locomotion routes around the expanded plasma bounds. While a
            // magnet has hold of the unit, suspend the motor instead of letting its
            // own pursuit movement carry it into the hazard; only field force and
            // existing momentum can move it there.
            if (!magneticallyDisplaced)
            {
                Vector3 steer = Hazard.SteerAway(
                    transform.position,
                    desiredMove,
                    hazardTurnBias);
                Vector3 desiredVelocity = steer * definition.MovementSpeed;
                Vector3 velocityDelta = Vector3.ClampMagnitude(
                    desiredVelocity - planarVelocity,
                    definition.Acceleration * Time.fixedDeltaTime);
                Vector3 motorPlanarVelocity = planarVelocity + velocityDelta;
                Vector3 safePlanarVelocity = Hazard.RedirectVelocity(
                    transform.position,
                    motorPlanarVelocity,
                    hazardTurnBias);
                body.linearVelocity = new Vector3(
                    safePlanarVelocity.x,
                    body.linearVelocity.y,
                    safePlanarVelocity.z);
                planarVelocity = safePlanarVelocity;
            }

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
            Transform enemyModel,
            Transform shield,
            Transform barRoot,
            Transform barFill,
            TextMesh label,
            LineRenderer attackRing,
            LineRenderer sightRing,
            LineRenderer telegraphLine)
        {
            definition = enemyDefinition;
            target = player;
            renderers = enemyRenderers;
            bodyMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                bodyMaterials[i] = renderers[i] == null ? null : renderers[i].material;
            }
            modelRoot = enemyModel;
            modelBasePosition = enemyModel == null ? Vector3.zero : enemyModel.localPosition;
            shieldVisual = shield;
            shieldMaterial = shieldVisual == null ? null : shieldVisual.GetComponent<Renderer>().material;
            // Plate polarity is rolled per unit so its colour tells the player which
            // anchor tears it off -- the opposite one.
            platePolarity = Random.value < 0.5f
                ? MagneticPolarity.Negative
                : MagneticPolarity.Positive;
            healthBarRoot = barRoot;
            healthFill = barFill;
            debugLabel = label;
            attackRangeRing = attackRing;
            perceptionRing = sightRing;
            aimLine = telegraphLine;
            baseColor = definition.Accent;
            health.Configure(definition.MaximumHealth, CombatFaction.Enemy);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            nextAttackTime = Time.time + 0.8f;
            hazardTurnBias = (GetEntityId().GetHashCode() & 1) == 0 ? 1f : -1f;
            configured = true;
            SetState(EnemyState.Acquiring);
        }

        public void NotifyMagneticForce(float magnitude, MagneticPolarity anchorPolarity)
        {
            if (!configured || magnitude < 8f)
            {
                return;
            }

            // Plasma is an environmental execution tool, not something enemies
            // should kill themselves on. A meaningful field hit opens a long enough
            // window for a robot dragged into plasma to take lethal damage.
            hazardVulnerableUntil = Mathf.Max(hazardVulnerableUntil, Time.time + 1.25f);

            if (definition.Archetype == EnemyArchetype.Shield)
            {
                // An anchor of the opposite polarity attracts the plate and rips it off
                // for good. A matching anchor only staggers the unit. The robot itself
                // never moves, so it keeps avoiding plasma even inside a field.
                if (!plateTornOff && anchorPolarity != platePolarity)
                {
                    TearOffPlate();
                }

                shieldExposedUntil = Mathf.Max(shieldExposedUntil, Time.time + 3.2f);
            }
            else
            {
                magnetHeldUntil = Time.time + 0.35f;
            }

            if (State != EnemyState.Dead && State != EnemyState.Telegraphing)
            {
                SetState(EnemyState.Displaced);
            }
        }

        private void TearOffPlate()
        {
            plateTornOff = true;

            if (shieldVisual == null)
            {
                return;
            }

            Transform plate = shieldVisual;
            shieldVisual = null;
            shieldMaterial = null;

            plate.SetParent(null, true);
            plate.gameObject.AddComponent<BoxCollider>();

            Rigidbody plateBody = plate.gameObject.AddComponent<Rigidbody>();
            plateBody.mass = 1.3f;
            plateBody.linearDamping = 0.35f;
            plateBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            plateBody.AddForce(
                (transform.forward * 4.5f + Vector3.up * 3.2f) * plateBody.mass,
                ForceMode.Impulse);
            plateBody.AddTorque(Random.insideUnitSphere * 6f, ForceMode.Impulse);

            plate.gameObject.AddComponent<MagneticTarget>().Configure(platePolarity, 1.6f);
            Destroy(plate.gameObject, 8f);

            FeedbackBus.Pulse(150f, 0.2f, 0.16f);
            CameraRig.Active?.AddTrauma(0.16f);
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
            if (modelRoot != null && !health.IsDead)
            {
                Vector3 localVelocity = transform.InverseTransformDirection(body.linearVelocity);
                float bob = Mathf.Sin(Time.time * 5f + GetEntityId().GetHashCode() * 0.01f) * 0.045f;
                modelRoot.localPosition = Vector3.Lerp(
                    modelRoot.localPosition,
                    modelBasePosition + Vector3.up * bob,
                    1f - Mathf.Exp(-12f * Time.deltaTime));
                Quaternion lean = Quaternion.Euler(
                    Mathf.Clamp(localVelocity.z * 0.65f, -6f, 6f),
                    0f,
                    Mathf.Clamp(-localVelocity.x * 1.4f, -10f, 10f));
                modelRoot.localRotation = Quaternion.Slerp(
                    modelRoot.localRotation,
                    lean,
                    1f - Mathf.Exp(-9f * Time.deltaTime));
            }

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
                Color plateColor = platePolarity == MagneticPolarity.Negative
                    ? RuntimeArt.Pull
                    : RuntimeArt.Push;
                Color shieldColor = ShieldExposed ? new Color(0.28f, 0.32f, 0.35f) : plateColor;
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

            if (healthBarRoot != null)
            {
                healthBarRoot.gameObject.SetActive(!health.IsDead);
                if (Camera.main != null)
                {
                    healthBarRoot.rotation = Camera.main.transform.rotation;
                }
            }

            if (healthFill != null)
            {
                float normalized = Mathf.Clamp01(health.Normalized);
                Vector3 scale = healthFill.localScale;
                scale.x = 1.08f * normalized;
                healthFill.localScale = scale;
                healthFill.localPosition = new Vector3(-0.54f * (1f - normalized), 0f, -0.025f);
            }

            bool telegraphing = State == EnemyState.Telegraphing;
            if (aimLine != null)
            {
                aimLine.gameObject.SetActive(telegraphing && target != null);
                if (telegraphing && target != null)
                {
                    float alpha = 0.35f + (Mathf.Sin(Time.time * 20f) + 1f) * 0.22f;
                    Color aimColor = new(baseColor.r, baseColor.g, baseColor.b, alpha);
                    aimLine.startColor = aimColor;
                    aimLine.endColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.05f);
                    aimLine.SetPosition(0, transform.position + Vector3.up * 1.35f + transform.forward * 0.65f);
                    aimLine.SetPosition(1, target.position + Vector3.up);
                }
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
                attackRangeRing.gameObject.SetActive(showDebug || telegraphing);
                Color ringColor = telegraphing
                    ? new Color(baseColor.r, baseColor.g, baseColor.b, 0.65f)
                    : new Color(RuntimeArt.Push.r, RuntimeArt.Push.g, RuntimeArt.Push.b, 0.35f);
                attackRangeRing.startColor = ringColor;
                attackRangeRing.endColor = ringColor;
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
