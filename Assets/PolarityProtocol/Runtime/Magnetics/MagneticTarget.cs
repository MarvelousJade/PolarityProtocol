using PolarityProtocol.AI;
using PolarityProtocol.Combat;
using UnityEngine;

namespace PolarityProtocol.Magnetics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MagneticTarget : MonoBehaviour
    {
        [SerializeField] private MagneticPolarity polarity = MagneticPolarity.Positive;
        [SerializeField, Min(0.05f)] private float forceMultiplier = 1f;

        private Rigidbody body;
        private Projectile projectile;
        private EnemyBrain enemy;

        public MagneticPolarity Polarity => polarity;
        public Vector3 LastForce { get; private set; }
        public float LastAffectedTime { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            projectile = GetComponent<Projectile>();
            enemy = GetComponent<EnemyBrain>();
        }

        private void LateUpdate()
        {
            if (Time.time - LastAffectedTime > 0.15f)
            {
                LastForce = Vector3.Lerp(LastForce, Vector3.zero, Time.deltaTime * 8f);
            }
        }

        public void Configure(MagneticPolarity targetPolarity, float multiplier)
        {
            polarity = targetPolarity;
            forceMultiplier = Mathf.Max(0.05f, multiplier);
        }

        public void ApplyMagneticForce(Vector3 force, GameObject anchorOwner, MagneticPolarity anchorPolarity)
        {
            if (body == null || body.isKinematic)
            {
                return;
            }

            Vector3 applied = force * forceMultiplier;
            body.AddForce(applied, ForceMode.Acceleration);
            LastForce = applied;
            LastAffectedTime = Time.time;
            enemy?.NotifyMagneticForce(applied.magnitude, anchorPolarity);

            if (projectile != null && anchorOwner != null)
            {
                projectile.RedirectByPlayer(anchorOwner);
            }
        }
    }
}

