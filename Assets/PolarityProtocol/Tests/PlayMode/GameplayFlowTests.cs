using System.Collections;
using NUnit.Framework;
using PolarityProtocol.Combat;
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

