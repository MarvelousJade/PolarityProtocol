using PolarityProtocol.Arena;
using PolarityProtocol.Combat;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Player
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        private const float MaxAimDistance = 140f;

        [SerializeField] private float damage = 27f;
        [SerializeField] private float projectileSpeed = 26f;
        [SerializeField] private float fireInterval = 0.23f;

        private readonly RaycastHit[] aimHits = new RaycastHit[8];
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

        /// <summary>
        /// World point under the crosshair. The muzzle sits beside the camera, so
        /// firing along the camera's forward vector lands shots off to one side --
        /// shots have to converge on this point instead.
        /// </summary>
        public Vector3 ResolveAimPoint(Camera camera, Vector3 muzzle)
        {
            if (camera == null)
            {
                return muzzle + transform.forward * MaxAimDistance;
            }

            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 aimPoint = ray.GetPoint(MaxAimDistance);
            float nearest = float.MaxValue;

            int count = Physics.RaycastNonAlloc(
                ray,
                aimHits,
                MaxAimDistance,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (aimHits[i].transform.root == transform.root || aimHits[i].distance >= nearest)
                {
                    continue;
                }

                nearest = aimHits[i].distance;
                aimPoint = aimHits[i].point;
            }

            // Anything practically on top of the muzzle would invert the shot.
            return Vector3.Distance(aimPoint, muzzle) < 1.2f
                ? muzzle + camera.transform.forward * MaxAimDistance
                : aimPoint;
        }

        private void Fire()
        {
            Camera camera = Camera.main;
            Vector3 chest = transform.position + Vector3.up * 1.25f;
            Vector3 aimDirection = (ResolveAimPoint(camera, chest) - chest).normalized;
            Vector3 spawn = chest + aimDirection * 0.9f;

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

