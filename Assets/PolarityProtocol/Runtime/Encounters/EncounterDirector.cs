using System.Collections;
using System.Collections.Generic;
using PolarityProtocol.AI;
using PolarityProtocol.Arena;
using PolarityProtocol.Combat;
using PolarityProtocol.Data;
using PolarityProtocol.Magnetics;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Encounters
{
    public sealed class EncounterDirector : MonoBehaviour
    {
        private readonly List<Health> livingEnemies = new();
        private EncounterDefinition[] encounters;
        private EnemyDefinition[] enemyDefinitions;
        private Transform player;
        private Coroutine sequence;

        public int CurrentEncounterIndex { get; private set; } = -1;
        public int EncounterCount => encounters?.Length ?? 0;
        public int LivingEnemyCount => livingEnemies.Count;
        public string CurrentTitle =>
            CurrentEncounterIndex >= 0 && CurrentEncounterIndex < EncounterCount
                ? encounters[CurrentEncounterIndex].DisplayName
                : "SYSTEM READY";
        public string CurrentObjective =>
            CurrentEncounterIndex >= 0 && CurrentEncounterIndex < EncounterCount
                ? encounters[CurrentEncounterIndex].Objective
                : "Deploy anchors. Rewrite the battlefield.";

        public void Configure(
            EncounterDefinition[] encounterData,
            EnemyDefinition[] definitions,
            Transform target)
        {
            encounters = encounterData;
            enemyDefinitions = definitions;
            player = target;
        }

        public void Begin()
        {
            if (sequence == null)
            {
                sequence = StartCoroutine(RunSequence());
            }
        }

        private IEnumerator RunSequence()
        {
            yield return new WaitForSeconds(0.75f);

            for (int encounterIndex = 0; encounterIndex < encounters.Length; encounterIndex++)
            {
                CurrentEncounterIndex = encounterIndex;
                EncounterDefinition encounter = encounters[encounterIndex];
                FeedbackBus.Pulse(260f + encounterIndex * 90f, 0.16f, 0.08f);

                yield return new WaitForSeconds(1.1f);

                for (int spawnIndex = 0; spawnIndex < encounter.Spawns.Length; spawnIndex++)
                {
                    EncounterDefinition.Spawn spawn = encounter.Spawns[spawnIndex];
                    EnemyDefinition definition = FindDefinition(spawn.archetype);
                    if (definition != null)
                    {
                        EnemyBrain enemy = EnemyFactory.Create(definition, player, spawn.position);
                        livingEnemies.Add(enemy.Health);
                        enemy.Health.Died += OnEnemyDied;
                    }

                    yield return new WaitForSeconds(0.24f);
                }

                while (livingEnemies.Count > 0)
                {
                    if (GameSession.Active == null || GameSession.Active.State == SessionState.Failed)
                    {
                        yield break;
                    }

                    livingEnemies.RemoveAll(health => health == null);
                    yield return null;
                }

                if (encounterIndex < encounters.Length - 1)
                {
                    yield return new WaitForSeconds(2.2f);
                }
            }

            yield return new WaitForSeconds(1f);
            GameSession.Active?.CompleteRun();
        }

        private EnemyDefinition FindDefinition(EnemyArchetype archetype)
        {
            for (int i = 0; i < enemyDefinitions.Length; i++)
            {
                if (enemyDefinitions[i] != null && enemyDefinitions[i].Archetype == archetype)
                {
                    return enemyDefinitions[i];
                }
            }

            return null;
        }

        private void OnEnemyDied(Health health, DamageInfo _)
        {
            health.Died -= OnEnemyDied;
            livingEnemies.Remove(health);
        }
    }

    public static class EnemyFactory
    {
        public static EnemyBrain Create(EnemyDefinition definition, Transform player, Vector3 position)
        {
            GameObject root = new($"{definition.Archetype} Unit");
            root.transform.position = position;

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.height = 2.2f;
            collider.radius = 0.55f;
            collider.center = new Vector3(0f, 1.1f, 0f);

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = definition.Archetype == EnemyArchetype.Shield ? 5.5f : 3.2f;
            body.linearDamping = 2.1f;
            body.angularDamping = 8f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            Health health = root.AddComponent<Health>();
            root.AddComponent<DamageReceiver>();
            EnemyBrain brain = root.AddComponent<EnemyBrain>();
            MagneticTarget magneticTarget = root.AddComponent<MagneticTarget>();
            bool shieldUnit = definition.Archetype == EnemyArchetype.Shield;
            // The shield robot braces against anchors -- only its plate answers the
            // field. Every other archetype can be dragged freely.
            magneticTarget.Configure(MagneticPolarity.Positive, 1f, shieldUnit);

            Transform model = new GameObject("Robot Model").transform;
            model.SetParent(root.transform, false);

            List<Renderer> renderers = new();
            GameObject torso = RuntimeArt.Primitive(
                PrimitiveType.Cylinder,
                "Torso",
                model,
                new Vector3(0f, 1.05f, 0f),
                new Vector3(0.68f, 0.72f, 0.68f),
                definition.Accent,
                false,
                0.55f);
            renderers.Add(torso.GetComponent<Renderer>());
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Chest Armour",
                model,
                new Vector3(0f, 1.08f, 0.48f),
                new Vector3(0.78f, 0.58f, 0.14f),
                RuntimeArt.Slate,
                false,
                0.1f);

            GameObject head = RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Head",
                model,
                new Vector3(0f, 1.93f, 0.06f),
                new Vector3(0.82f, 0.5f, 0.72f),
                definition.Accent,
                false,
                0.45f);
            renderers.Add(head.GetComponent<Renderer>());

            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Visor",
                model,
                new Vector3(0f, 1.98f, 0.43f),
                new Vector3(0.58f, 0.12f, 0.05f),
                Color.white,
                false,
                3f);

            RuntimeArt.Primitive(
                PrimitiveType.Cylinder,
                "Left Leg",
                model,
                new Vector3(-0.29f, 0.28f, 0f),
                new Vector3(0.22f, 0.38f, 0.22f),
                RuntimeArt.Dark,
                false);
            RuntimeArt.Primitive(
                PrimitiveType.Cylinder,
                "Right Leg",
                model,
                new Vector3(0.29f, 0.28f, 0f),
                new Vector3(0.22f, 0.38f, 0.22f),
                RuntimeArt.Dark,
                false);

            if (definition.Archetype == EnemyArchetype.Shooter)
            {
                GameObject cannon = RuntimeArt.Primitive(
                    PrimitiveType.Cylinder,
                    "Projectile Cannon",
                    model,
                    new Vector3(0.68f, 1.32f, 0.35f),
                    new Vector3(0.2f, 0.42f, 0.2f),
                    definition.Accent,
                    false,
                    1.1f);
                cannon.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            }
            else if (definition.Archetype == EnemyArchetype.Chaser)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject blade = RuntimeArt.Primitive(
                        PrimitiveType.Cube,
                        side < 0 ? "Left Ram" : "Right Ram",
                        model,
                        new Vector3(side * 0.68f, 1.05f, 0.38f),
                        new Vector3(0.13f, 0.6f, 0.16f),
                        definition.Accent,
                        false,
                        0.8f);
                    blade.transform.localEulerAngles = new Vector3(35f, 0f, side * 18f);
                }
            }

            Transform shield = null;
            if (definition.Archetype == EnemyArchetype.Shield)
            {
                GameObject shieldObject = RuntimeArt.Primitive(
                    PrimitiveType.Cube,
                    "Directional Shield",
                    model,
                    new Vector3(0f, 1.15f, 0.78f),
                    new Vector3(1.5f, 1.8f, 0.12f),
                    RuntimeArt.Gold,
                    false,
                    1.2f);
                shield = shieldObject.transform;
            }

            TextMesh label = RuntimeArt.Label(
                root.transform,
                definition.Archetype.ToString(),
                new Vector3(0f, 2.85f, 0f),
                Color.white,
                32,
                0.055f);
            label.gameObject.SetActive(false);

            Transform healthBarRoot = new GameObject("Health Bar").transform;
            healthBarRoot.SetParent(root.transform, false);
            healthBarRoot.localPosition = new Vector3(0f, 2.62f, 0f);
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Health Back",
                healthBarRoot,
                Vector3.zero,
                new Vector3(1.18f, 0.12f, 0.045f),
                new Color(0.015f, 0.025f, 0.035f),
                false);
            GameObject healthFillObject = RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Health Fill",
                healthBarRoot,
                new Vector3(0f, 0f, -0.025f),
                new Vector3(1.08f, 0.065f, 0.035f),
                definition.Accent,
                false,
                1.2f);

            LineRenderer attackRing = RuntimeArt.Ring(root.transform, definition.AttackRange, RuntimeArt.Push, 0.035f, 48);
            LineRenderer sightRing = RuntimeArt.Ring(root.transform, definition.PerceptionRange, new Color(1f, 1f, 1f, 0.22f), 0.018f, 72);
            attackRing.gameObject.SetActive(false);
            sightRing.gameObject.SetActive(false);

            LineRenderer aimLine = null;
            if (definition.Archetype == EnemyArchetype.Shooter)
            {
                GameObject aimObject = new("Shot Telegraph");
                aimObject.transform.SetParent(root.transform, false);
                aimLine = aimObject.AddComponent<LineRenderer>();
                aimLine.useWorldSpace = true;
                aimLine.positionCount = 2;
                aimLine.startWidth = 0.045f;
                aimLine.endWidth = 0.012f;
                aimLine.sharedMaterial = RuntimeArt.Material(Color.white, 1.2f, true);
                aimObject.SetActive(false);
            }

            brain.Configure(
                definition,
                player,
                renderers.ToArray(),
                model,
                shield,
                healthBarRoot,
                healthFillObject.transform,
                label,
                attackRing,
                sightRing,
                aimLine);
            return brain;
        }
    }
}
