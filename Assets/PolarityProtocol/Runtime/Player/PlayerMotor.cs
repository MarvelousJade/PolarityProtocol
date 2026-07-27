using PolarityProtocol.Arena;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float movementSpeed = 7.5f;
        [SerializeField] private float sprintSpeed = 10.5f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float rotationSpeed = 16f;
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private float dashCooldown = 1.1f;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private Vector3 verticalVelocity;
        private Vector3 dashDirection;
        private TrailRenderer dashTrail;
        private float dashRemaining;
        private float dashCooldownRemaining;
        private Vector3 spawnPoint;
        private Vector3 aimFacing;
        private float aimFacingUntil;

        public float DashCooldownNormalized => dashCooldown <= 0f
            ? 0f
            : Mathf.Clamp01(dashCooldownRemaining / dashCooldown);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawnPoint = transform.position;
        }

        private void Start()
        {
            dashTrail = GetComponentInChildren<TrailRenderer>();
            if (dashTrail != null)
            {
                dashTrail.emitting = false;
            }
        }

        private void Update()
        {
            if (GameSession.Active == null || !GameSession.Active.IsRunning)
            {
                return;
            }

            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - Time.deltaTime);
            Vector2 input = Vector2.ClampMagnitude(LegacyInput.Move, 1f);

            Transform cameraTransform = Camera.main == null ? transform : Camera.main.transform;
            Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            Vector3 desiredDirection = (cameraForward * input.y + cameraRight * input.x);
            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }

            if (LegacyInput.DashPressed && dashCooldownRemaining <= 0f)
            {
                dashDirection = desiredDirection.sqrMagnitude > 0.05f ? desiredDirection : transform.forward;
                dashRemaining = dashDuration;
                dashCooldownRemaining = dashCooldown;
                CameraRig.Active?.AddTrauma(0.22f);
                FeedbackBus.Pulse(90f, 0.1f, 0.13f);
            }

            if (dashRemaining > 0f)
            {
                dashRemaining -= Time.deltaTime;
                horizontalVelocity = dashDirection * dashSpeed;
            }
            else
            {
                float targetSpeed = LegacyInput.SprintHeld ? sprintSpeed : movementSpeed;
                Vector3 desiredVelocity = desiredDirection * targetSpeed;
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    desiredVelocity,
                    acceleration * Time.deltaTime);
            }

            if (dashTrail != null)
            {
                dashTrail.emitting = dashRemaining > 0f;
            }

            // Shooting wins over movement for facing, so shots leave the front of the
            // model instead of punching out through its back while backpedalling.
            Vector3 facing = Time.time < aimFacingUntil ? aimFacing : desiredDirection;
            if (facing.sqrMagnitude > 0.05f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    1f - Mathf.Exp(-rotationSpeed * Time.deltaTime));
            }

            if (controller.isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }
            else
            {
                verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
            }

            controller.Move((horizontalVelocity + verticalVelocity) * Time.deltaTime);

            if (transform.position.y < -8f)
            {
                TeleportToSpawn();
            }
        }

        /// <summary>
        /// Turns the character to face a shot for a moment, overriding movement facing.
        /// </summary>
        public void FaceAim(Vector3 direction)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flat.sqrMagnitude < 0.01f)
            {
                return;
            }

            aimFacing = flat.normalized;
            aimFacingUntil = Time.time + 0.45f;
        }

        public void TeleportToSpawn()
        {
            controller.enabled = false;
            transform.position = spawnPoint;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = Vector3.zero;
            controller.enabled = true;
        }
    }
}
