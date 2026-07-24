using UnityEngine;

namespace PolarityProtocol.Combat
{
    [DisallowMultipleComponent]
    public sealed class DamageReceiver : MonoBehaviour
    {
        private Health health;

        public Health Health => health;

        private void Awake()
        {
            health = GetComponentInParent<Health>();
        }

        public bool Receive(DamageInfo info)
        {
            return health != null && health.TakeDamage(info);
        }
    }
}

