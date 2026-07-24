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
            magneticTarget.Configure(MagneticPolarity.Positive, definition.Archetype == EnemyArchetype.Shield ? 0.72f : 1f);

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

            LineRenderer attackRing = RuntimeArt.Ring(root.transform, definition.AttackRange, RuntimeArt.Push, 0.035f, 48);
            LineRenderer sightRing = RuntimeArt.Ring(root.transform, definition.PerceptionRange, new Color(1f, 1f, 1f, 0.22f), 0.018f, 72);
            attackRing.gameObject.SetActive(false);
            sightRing.gameObject.SetActive(false);

            brain.Configure(
                definition,
                player,
                renderers.ToArray(),
                shield,
                label,
                attackRing,
                sightRing);
            return brain;
        }
    }
}

