using NUnit.Framework;
using PolarityProtocol.Data;
using PolarityProtocol.Magnetics;
using UnityEngine;

namespace PolarityProtocol.Tests
{
    public sealed class MagneticForceSolverTests
    {
        [Test]
        public void OpposingPolarities_AttractTowardAnchor()
        {
            Vector3 force = MagneticForceSolver.Calculate(
                Vector3.zero,
                Vector3.right * 3f,
                MagneticPolarity.Negative,
                MagneticPolarity.Positive,
                40f,
                8f,
                0.5f);

            Assert.That(force.x, Is.LessThan(0f));
            Assert.That(force.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void MatchingPolarities_RepelAwayFromAnchor()
        {
            Vector3 force = MagneticForceSolver.Calculate(
                Vector3.zero,
                Vector3.right * 3f,
                MagneticPolarity.Positive,
                MagneticPolarity.Positive,
                40f,
                8f,
                0.5f);

            Assert.That(force.x, Is.GreaterThan(0f));
        }

        [Test]
        public void Force_IsZeroOutsideRadius()
        {
            Vector3 force = MagneticForceSolver.Calculate(
                Vector3.zero,
                Vector3.forward * 9f,
                MagneticPolarity.Negative,
                MagneticPolarity.Positive,
                40f,
                8f,
                0.5f);

            Assert.That(force, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Force_RemainsMeaningfulAtVisibleRadius()
        {
            Vector3 force = MagneticForceSolver.Calculate(
                Vector3.zero,
                Vector3.right * 7.5f,
                MagneticPolarity.Negative,
                MagneticPolarity.Positive,
                78f,
                7.5f,
                0.8f,
                1.1f);

            Assert.That(force.magnitude, Is.GreaterThan(8f));
        }

        [Test]
        public void MinimumDistance_PreventsNonFiniteForce()
        {
            Vector3 force = MagneticForceSolver.Calculate(
                Vector3.zero,
                Vector3.zero,
                MagneticPolarity.Negative,
                MagneticPolarity.Positive,
                40f,
                8f,
                0.8f);

            Assert.That(float.IsNaN(force.x) || float.IsInfinity(force.x), Is.False);
            Assert.That(force.magnitude, Is.LessThanOrEqualTo(40f));
        }

        [Test]
        public void Force_DecreasesWithDistance()
        {
            float near = MagneticForceSolver.Calculate(
                Vector3.zero,
                Vector3.right * 2f,
                MagneticPolarity.Negative,
                MagneticPolarity.Positive,
                40f,
                8f,
                0.5f).magnitude;
            float far = MagneticForceSolver.Calculate(
                Vector3.zero,
                Vector3.right * 6f,
                MagneticPolarity.Negative,
                MagneticPolarity.Positive,
                40f,
                8f,
                0.5f).magnitude;

            Assert.That(near, Is.GreaterThan(far));
        }

        [Test]
        public void AbilityDefaults_AreWithinValidRanges()
        {
            AbilityDefinition ability = ScriptableObject.CreateInstance<AbilityDefinition>();
            ability.ConfigureDemoDefaults();

            Assert.That(ability.EnergyCost, Is.InRange(0f, ability.MaximumEnergy));
            Assert.That(ability.MinimumDistance, Is.LessThan(ability.Radius));
            Assert.That(ability.MaximumActiveAnchors, Is.InRange(1, 4));

            Object.DestroyImmediate(ability);
        }
    }
}

