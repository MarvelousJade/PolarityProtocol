using System.Collections;
using NUnit.Framework;
using PolarityProtocol.AI;
using PolarityProtocol.Arena;
using PolarityProtocol.Combat;
using PolarityProtocol.Data;
using PolarityProtocol.Encounters;
using PolarityProtocol.Magnetics;
using PolarityProtocol.Player;
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
        public IEnumerator OppositePolarityField_CurvesProjectileBackToShooter()
        {
            GameObject poolObject = new("Projectile Pool");
            ProjectilePool pool = poolObject.AddComponent<ProjectilePool>();
            GameObject enemy = new("Enemy Owner");
            enemy.transform.position = Vector3.back * 20f;
            GameObject player = new("Player Owner");

            Projectile projectile = pool.Spawn(
                CombatFaction.Enemy,
                enemy,
                Vector3.right * 2f,
                Vector3.forward * 10f,
                10f,
                Color.blue,
                MagneticPolarity.Negative);
            Assert.That(projectile.Velocity.z, Is.EqualTo(10f).Within(0.01f));

            AbilityDefinition ability = ScriptableObject.CreateInstance<AbilityDefinition>();
            ability.ConfigureDemoDefaults();

            GameObject matchingAnchorObject = new("Matching Anchor");
            MagneticAnchor matchingAnchor = matchingAnchorObject.AddComponent<MagneticAnchor>();
            matchingAnchor.Initialize(ability, MagneticPolarity.Negative, player);
            yield return new WaitForFixedUpdate();

            Assert.That(projectile.Faction, Is.EqualTo(CombatFaction.Enemy));
            Assert.That(projectile.WasRedirected, Is.False);

            Object.Destroy(matchingAnchorObject);
            yield return null;

            GameObject oppositeAnchorObject = new("Opposite Anchor");
            MagneticAnchor oppositeAnchor = oppositeAnchorObject.AddComponent<MagneticAnchor>();
            oppositeAnchor.Initialize(ability, MagneticPolarity.Positive, player);
            yield return new WaitForFixedUpdate();

            Assert.That(projectile.Faction, Is.EqualTo(CombatFaction.Player));
            Assert.That(projectile.WasRedirected, Is.True);
            Assert.That(projectile.Polarity, Is.EqualTo(MagneticPolarity.Negative));

            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Vector3 towardShooter = (enemy.transform.position + Vector3.up - projectile.transform.position).normalized;
            Assert.That(Vector3.Dot(projectile.Velocity.normalized, towardShooter), Is.GreaterThan(0.7f));

            projectile.Release();
            Object.Destroy(oppositeAnchorObject);
            Object.Destroy(ability);
            Object.Destroy(enemy);
            Object.Destroy(player);
            Object.Destroy(poolObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Aim_ConvergesOnCrosshairTargetNotCameraForward()
        {
            // Wall straight ahead of the crosshair, camera offset to the shoulder.
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1.25f, 20f);
            wall.transform.localScale = new Vector3(30f, 8f, 1f);

            GameObject shooter = new("Shooter");
            shooter.transform.position = Vector3.zero;
            PlayerCombat combat = shooter.AddComponent<PlayerCombat>();

            GameObject cameraObject = new("Aim Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(1.35f, 1.25f, -8f);
            cameraObject.transform.rotation = Quaternion.identity;

            // Colliders were repositioned after creation; without this the raycast
            // queries their old pose and misses.
            Physics.SyncTransforms();
            yield return null;

            Vector3 chest = shooter.transform.position + Vector3.up * 1.25f;
            Vector3 aimPoint = combat.ResolveAimPoint(camera, chest);

            // The crosshair ray leaves the camera, so its wall hit keeps the camera's
            // lateral offset -- the shot must lean across to meet it.
            Assert.That(aimPoint.z, Is.EqualTo(19.5f).Within(0.6f));
            Assert.That(aimPoint.x, Is.EqualTo(1.35f).Within(0.3f));

            Vector3 aimDirection = (aimPoint - chest).normalized;
            Assert.That(aimDirection.x, Is.GreaterThan(0.02f));

            Object.Destroy(wall);
            Object.Destroy(shooter);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Aim_IgnoresObstaclesBehindThePlayer()
        {
            // Crate sitting between the camera and the player's back.
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.transform.position = new Vector3(1.35f, 1.25f, -4f);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1.25f, 20f);
            wall.transform.localScale = new Vector3(30f, 8f, 1f);

            GameObject shooter = new("Shooter");
            shooter.transform.position = Vector3.zero;
            PlayerCombat combat = shooter.AddComponent<PlayerCombat>();

            GameObject cameraObject = new("Aim Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(1.35f, 1.25f, -8f);
            cameraObject.transform.rotation = Quaternion.identity;

            Physics.SyncTransforms();
            yield return null;

            Vector3 chest = shooter.transform.position + Vector3.up * 1.25f;
            Vector3 aimPoint = combat.ResolveAimPoint(camera, chest);
            Vector3 aimDirection = (aimPoint - chest).normalized;

            // The crate must not become the target -- that fires the shot backwards.
            Assert.That(aimPoint.z, Is.GreaterThan(chest.z));
            Assert.That(aimDirection.z, Is.GreaterThan(0f));

            Object.Destroy(crate);
            Object.Destroy(wall);
            Object.Destroy(shooter);
            Object.Destroy(cameraObject);
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
            definition.Configure(
                EnemyArchetype.Shield,
                105f,
                4.2f,
                2.6f,
                24f,
                2.3f,
                MagneticPolarity.Positive,
                Color.yellow);
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
        public IEnumerator Hazard_AvoidanceUsesVisualFootprintBeyondDamageTrigger()
        {
            GameObject hazardObject = new("Test Plasma Footprint");
            BoxCollider trigger = hazardObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.44f, 2f, 3.44f);
            Hazard hazard = hazardObject.AddComponent<Hazard>();
            hazard.ConfigureAvoidanceFootprint(new Vector2(4f, 4f));

            Physics.SyncTransforms();
            yield return null;

            Vector3 safePosition = Hazard.ResolveSafePosition(new Vector3(-3.2f, 0f, 0f));

            Assert.That(safePosition.x, Is.LessThan(-3.35f));
            Assert.That(trigger.bounds.min.x, Is.EqualTo(-1.72f).Within(0.01f));

            Object.Destroy(hazardObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShooterMotor_CorrectsPositionOutsideVisualHazardFootprint()
        {
            GameObject hazardObject = new("Test Plasma Footprint");
            BoxCollider trigger = hazardObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1f, 0f);
            trigger.size = new Vector3(3.44f, 2f, 3.44f);
            Hazard hazard = hazardObject.AddComponent<Hazard>();
            hazard.ConfigureAvoidanceFootprint(new Vector2(4f, 4f));

            GameObject player = new("Player Target");
            Health playerHealth = player.AddComponent<Health>();
            playerHealth.Configure(100f, CombatFaction.Player);
            player.transform.position = new Vector3(6f, 0f, 0f);

            GameObject systems = new("Test Runtime Systems");
            EncounterDirector director = systems.AddComponent<EncounterDirector>();
            director.Configure(
                new EncounterDefinition[0],
                new EnemyDefinition[0],
                player.transform);
            GameSession session = systems.AddComponent<GameSession>();
            session.Configure(playerHealth, director);
            session.BeginRun();

            GameObject unit = new("Shooter Unit");
            unit.transform.position = new Vector3(-2.5f, 0f, 0f);
            unit.AddComponent<CapsuleCollider>();
            Rigidbody body = unit.AddComponent<Rigidbody>();
            body.useGravity = false;
            unit.AddComponent<Health>();
            EnemyBrain brain = unit.AddComponent<EnemyBrain>();
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.Configure(
                EnemyArchetype.Shooter,
                48f,
                4.6f,
                18f,
                14f,
                2.2f,
                MagneticPolarity.Positive,
                Color.magenta);
            brain.Configure(
                definition,
                player.transform,
                new Renderer[0],
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(body.position.x, Is.LessThan(-3.35f));

            Object.Destroy(hazardObject);
            Object.Destroy(unit);
            Object.Destroy(player);
            Object.Destroy(systems);
            Object.Destroy(definition);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Enemy_HazardDamageRequiresRecentMagneticForce()
        {
            GameObject hazardObject = new("Test Plasma");
            BoxCollider trigger = hazardObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(4f, 2f, 4f);
            hazardObject.AddComponent<Hazard>();

            GameObject unit = new("Chaser Unit");
            CapsuleCollider unitCollider = unit.AddComponent<CapsuleCollider>();
            unitCollider.height = 2.2f;
            unitCollider.radius = 0.55f;
            Rigidbody body = unit.AddComponent<Rigidbody>();
            body.isKinematic = true;
            Health health = unit.AddComponent<Health>();
            EnemyBrain brain = unit.AddComponent<EnemyBrain>();

            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.Configure(
                EnemyArchetype.Chaser,
                65f,
                6.2f,
                2.3f,
                18f,
                1.7f,
                MagneticPolarity.Negative,
                Color.red);
            brain.Configure(
                definition,
                null,
                new Renderer[0],
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(health.Current, Is.EqualTo(health.Maximum));

            brain.NotifyMagneticForce(40f, MagneticPolarity.Negative);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(health.Current, Is.LessThan(health.Maximum));

            Object.Destroy(hazardObject);
            Object.Destroy(unit);
            Object.Destroy(definition);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BlueEnemy_IsSpawnedWithNegativePolarityAndPulledByRed()
        {
            GameObject player = new("Player");
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.Configure(
                EnemyArchetype.Chaser,
                65f,
                6.2f,
                2.3f,
                18f,
                1.7f,
                MagneticPolarity.Negative,
                Color.cyan);

            EnemyBrain enemy = EnemyFactory.Create(definition, player.transform, Vector3.right * 3f);
            MagneticTarget target = enemy.GetComponent<MagneticTarget>();
            Vector3 force = MagneticForceSolver.Calculate(
                Vector3.zero,
                enemy.transform.position,
                MagneticPolarity.Positive,
                target.Polarity,
                40f,
                8f,
                0.5f);

            Assert.That(target.Polarity, Is.EqualTo(MagneticPolarity.Negative));
            Assert.That(force.x, Is.LessThan(0f), "The red positive anchor should pull a blue negative enemy.");

            Object.Destroy(enemy.gameObject);
            Object.Destroy(player);
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

