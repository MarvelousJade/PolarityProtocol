using PolarityProtocol.Arena;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Player
{
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private float distance = 8.5f;
        [SerializeField] private float height = 1.6f;
        [SerializeField] private float sensitivity = 2.4f;
        [SerializeField] private float smoothing = 14f;
        [SerializeField] private float collisionRadius = 0.25f;

        private Transform target;
        private float yaw;
        private float pitch = 18f;
        private float trauma;
        private Vector3 smoothVelocity;

        public static CameraRig Active { get; private set; }

        private void Awake()
        {
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (GameSession.Active != null && GameSession.Active.IsRunning)
            {
                Vector2 look = LegacyInput.Look;
                yaw += look.x * sensitivity;
                pitch = Mathf.Clamp(pitch - look.y * sensitivity, -8f, 58f);

                if (look.sqrMagnitude < 0.001f)
                {
                    Vector3 planarVelocity = target.forward;
                    float targetYaw = Mathf.Atan2(planarVelocity.x, planarVelocity.z) * Mathf.Rad2Deg;
                    yaw = Mathf.LerpAngle(yaw, targetYaw, Time.deltaTime * 0.35f);
                }
            }

            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + Vector3.up * height;
            Vector3 desired = focus - orbit * Vector3.forward * distance;
            Vector3 castDirection = desired - focus;
            float castDistance = castDirection.magnitude;

            if (Physics.SphereCast(
                    focus,
                    collisionRadius,
                    castDirection.normalized,
                    out RaycastHit hit,
                    castDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore) &&
                hit.transform.root != target.root)
            {
                desired = focus + castDirection.normalized * Mathf.Max(1.2f, hit.distance - 0.25f);
            }

            trauma = Mathf.Max(0f, trauma - Time.deltaTime * 1.5f);
            float shake = trauma * trauma;
            Vector3 noise = new(
                (Mathf.PerlinNoise(Time.time * 31f, 0.2f) - 0.5f) * shake,
                (Mathf.PerlinNoise(0.4f, Time.time * 29f) - 0.5f) * shake,
                0f);
            desired += noise * 0.8f;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref smoothVelocity,
                1f / Mathf.Max(1f, smoothing));
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }

        public void Follow(Transform followTarget)
        {
            target = followTarget;
            yaw = target.eulerAngles.y;
            transform.position = target.position + new Vector3(0f, height + 3f, -distance);
        }

        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }
    }
}

