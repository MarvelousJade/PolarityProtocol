using PolarityProtocol.Arena;
using PolarityProtocol.Combat;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Player
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private float damage = 27f;
        [SerializeField] private float projectileSpeed = 26f;
        [SerializeField] private float fireInterval = 0.23f;

        private float cooldown;

        public float CooldownRemaining => cooldown;

        private void Update()
        {
            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);

            if (GameSession.Active == null || !GameSession.Active.IsRunning)
            {
                return;
            }

            if (LegacyInput.AttackPressed && cooldown <= 0f)
            {
                Fire();
            }
        }

        private void Fire()
        {
            Camera camera = Camera.main;
            Vector3 aimDirection = camera == null ? transform.forward : camera.transform.forward;
            Vector3 spawn = transform.position + Vector3.up * 1.25f + aimDirection * 0.9f;

            ProjectilePool.Active?.Spawn(
                CombatFaction.Player,
                gameObject,
                spawn,
                aimDirection.normalized * projectileSpeed,
                damage,
                RuntimeArt.Gold);

            cooldown = fireInterval;
            FeedbackBus.Pulse(680f, 0.045f, 0.07f);
            CameraRig.Active?.AddTrauma(0.055f);
        }
    }
}

