using UnityEngine;

namespace PolarityProtocol.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerPresentation : MonoBehaviour
    {
        private CharacterController controller;
        private Transform model;
        private Transform fieldCoil;
        private Vector3 baseLocalPosition;
        private Quaternion currentLean = Quaternion.identity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (model == null)
            {
                return;
            }

            Vector3 localVelocity = transform.InverseTransformDirection(controller.velocity);
            float planarSpeed = new Vector2(localVelocity.x, localVelocity.z).magnitude;
            float bob = controller.isGrounded && planarSpeed > 0.3f
                ? Mathf.Sin(Time.time * Mathf.Lerp(7f, 12f, Mathf.Clamp01(planarSpeed / 10f))) * 0.035f
                : 0f;
            Vector3 targetPosition = baseLocalPosition + Vector3.up * bob;
            model.localPosition = Vector3.Lerp(
                model.localPosition,
                targetPosition,
                1f - Mathf.Exp(-14f * Time.deltaTime));

            Quaternion targetLean = Quaternion.Euler(
                Mathf.Clamp(localVelocity.z * 0.6f, -5f, 5f),
                0f,
                Mathf.Clamp(-localVelocity.x * 1.5f, -11f, 11f));
            currentLean = Quaternion.Slerp(
                currentLean,
                targetLean,
                1f - Mathf.Exp(-9f * Time.deltaTime));
            model.localRotation = currentLean;

            if (fieldCoil != null)
            {
                fieldCoil.Rotate(Vector3.up, (120f + planarSpeed * 18f) * Time.deltaTime, Space.Self);
            }
        }

        public void Configure(Transform modelRoot, Transform rotatingFieldCoil)
        {
            model = modelRoot;
            fieldCoil = rotatingFieldCoil;
            baseLocalPosition = modelRoot.localPosition;
        }
    }
}

