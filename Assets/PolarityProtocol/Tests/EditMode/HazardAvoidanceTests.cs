using NUnit.Framework;
using PolarityProtocol.Combat;
using UnityEngine;

namespace PolarityProtocol.Tests
{
    public sealed class HazardAvoidanceTests
    {
        private static readonly Bounds HazardBounds = new(
            Vector3.zero,
            new Vector3(4f, 2f, 4f));

        [Test]
        public void SteerAway_RoutesAnInwardMoveAroundHazard()
        {
            Vector3 steered = Hazard.SteerAwayFromBounds(
                new Vector3(-4f, 0f, 0f),
                Vector3.right,
                HazardBounds,
                1f);

            Assert.That(steered.x, Is.LessThan(0f));
            Assert.That(Mathf.Abs(steered.z), Is.GreaterThan(0.1f));
        }

        [Test]
        public void SteerAway_DoesNotAlterMovementAwayFromHazard()
        {
            Vector3 steered = Hazard.SteerAwayFromBounds(
                new Vector3(-4f, 0f, 0f),
                Vector3.left,
                HazardBounds,
                1f);

            Assert.That(steered, Is.EqualTo(Vector3.left));
        }

        [Test]
        public void SteerAway_ExpelsEnemyCentreBeforeColliderOverlapsTrigger()
        {
            // The trigger edge is x = -2. A normal enemy capsule reaches it
            // when its centre is around x = -2.55, so this point must already
            // be treated as blocked even though the centre is outside the box.
            Vector3 steered = Hazard.SteerAwayFromBounds(
                new Vector3(-2.75f, 0f, 0f),
                Vector3.right,
                HazardBounds,
                1f);

            Assert.That(steered, Is.EqualTo(Vector3.left));
        }

        [Test]
        public void RedirectVelocity_TurnsInwardMomentumBeforeHazardEdge()
        {
            Vector3 redirected = Hazard.RedirectVelocityFromBounds(
                new Vector3(-4f, 0f, 0f),
                Vector3.right * 6f,
                HazardBounds,
                1f);

            Assert.That(redirected.x, Is.LessThan(0f));
            Assert.That(Mathf.Abs(redirected.z), Is.GreaterThan(1f));
            Assert.That(redirected.magnitude, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void RedirectVelocity_DoesNotAlterOutwardMomentum()
        {
            Vector3 velocity = Vector3.left * 6f;
            Vector3 redirected = Hazard.RedirectVelocityFromBounds(
                new Vector3(-4f, 0f, 0f),
                velocity,
                HazardBounds,
                1f);

            Assert.That(redirected, Is.EqualTo(velocity));
        }

        [Test]
        public void ResolveSafeSpawn_MovesAuthoredPointOutsideHazardClearance()
        {
            Vector3 safePosition = Hazard.ResolveSafeSpawn(Vector3.zero, HazardBounds);

            Assert.That(safePosition.x, Is.LessThan(-3.1f));
            Assert.That(safePosition.z, Is.EqualTo(0f));
        }
    }
}
