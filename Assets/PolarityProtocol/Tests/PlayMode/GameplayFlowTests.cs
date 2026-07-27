using System.Collections;
using NUnit.Framework;
using PolarityProtocol.AI;
using PolarityProtocol.Combat;
using PolarityProtocol.Data;
using PolarityProtocol.Magnetics;
using UnityEngine;
using UnityEngine.TestTools;

namespace PolarityProtocol.Tests
{
    public sealed class GameplayFlowTests
    {
        [UnityTest]
        public IEnumerator Health_AppliesDamageAndRaisesDeath()
        {
            GameObject target = new("Target");
            Health health = target.AddComponent<Health>();
            health.Configure(25f, CombatFaction.Enemy);
            bool died = false;
            health.Died += (_, _) => died = true;

            bool firstApplied = health.TakeDamage(new DamageInfo(
                10f,
                null,
                Vector3.zero,
                Vector3.forward,
                DamageType.Kinetic));
            bool secondApplied = health.TakeDamage(new DamageInfo(
                20f,
                null,
                Vector3.zero,
                Vector3.forward,
                DamageType.Kinetic));

            Assert.That(firstApplied, Is.True);
            Assert.That(secondApplied, Is.True);
            Assert.That(health.Current, Is.EqualTo(0f));
            Assert.That(died, Is.True);

            Object.Destroy(target);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RedirectedProjectile_ChangesFactionAndOwnership()
        {
            GameObject poolObject = new("Projectile Pool");
            ProjectilePool pool = poolObject.AddComponent<ProjectilePool>();
            GameObject enemy = new("Enemy Owner");
            GameObject player = new("Player Owner");

            Projectile projectile = pool.Spawn(
                CombatFaction.Enemy,
                enemy,
                Vector3.zero,
                Vector3.forward * 10f,
                10f,
                Color.red);
            projectile.RedirectByPlayer(player);

            Assert.That(projectile.Faction, Is.EqualTo(CombatFaction.Player));
            Assert.That(projectile.WasRedirected, Is.True);

            projectile.Release();
            Object.Destroy(enemy);
            Object.Destroy(player);
            Object.Destroy(poolObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShieldPlate_IsTornOffByOppositePolarityOnly()
        {
            GameObject unit = new("Shield Unit");
            unit.AddComponent<Rigidbody>();
            unit.AddComponent<Health>();
            EnemyBrain brain = unit.AddComponent<EnemyBrain>();

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.transform.SetParent(unit.transform, false);

            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.Configure(EnemyArchetype.Shield, 105f, 4.2f, 2.6f, 24f, 2.3f, Color.yellow);
            brain.Configure(
                definition,
                null,
                new Renderer[0],
                null,
                plate.transform,
                null,
                null,
                null,
                null,
                null,
                null);

            // A matching anchor staggers the unit but leaves the plate bolted on.
            brain.NotifyMagneticForce(40f, brain.PlatePolarity);
            Assert.That(plate.transform.parent, Is.EqualTo(unit.transform));

            // The opposite anchor attracts the plate and strips it permanently.
            brain.NotifyMagneticForce(40f, brain.PlatePolarity.Opposite());
            yield return null;

            Assert.That(plate.transform.parent, Is.Null);
            Assert.That(brain.ShieldExposed, Is.True);

            Object.Destroy(plate);
            Object.Destroy(unit);
            Object.Destroy(definition);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProjectilePool_ReusesReleasedProjectile()
        {
            GameObject poolObject = new("Projectile Pool");
            ProjectilePool pool = poolObject.AddComponent<ProjectilePool>();
            GameObject owner = new("Owner");

            Projectile first = pool.Spawn(
                CombatFaction.Player,
                owner,
                Vector3.zero,
                Vector3.forward,
                1f,
                Color.cyan);
            first.Release();
            Projectile second = pool.Spawn(
                CombatFaction.Player,
                owner,
                Vector3.zero,
                Vector3.forward,
                1f,
                Color.cyan);

            Assert.That(second, Is.SameAs(first));
            Assert.That(pool.TotalCreated, Is.EqualTo(1));

            second.Release();
            Object.Destroy(owner);
            Object.Destroy(poolObject);
            yield return null;
        }
    }
}

