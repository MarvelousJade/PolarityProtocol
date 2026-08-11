using System;
using System.Linq;
using PolarityProtocol.Abilities;
using PolarityProtocol.Combat;
using PolarityProtocol.Data;
using PolarityProtocol.Encounters;
using PolarityProtocol.Magnetics;
using PolarityProtocol.Player;
using PolarityProtocol.UI;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Arena
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ArenaBootstrap : MonoBehaviour
    {
        private Transform arenaRoot;

        private void Start()
        {
            if (GameSession.Active != null)
            {
                return;
            }

            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            Physics.defaultSolverIterations = 8;
            Physics.defaultSolverVelocityIterations = 2;
            RenderSettings.ambientLight = new Color(0.16f, 0.22f, 0.28f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.025f, 0.045f, 0.07f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.008f;

            arenaRoot = new GameObject("Arena Geometry").transform;
            CreateArena();

            GameObject systems = new("Runtime Systems");
            systems.AddComponent<AudioSource>();
            systems.AddComponent<FeedbackBus>();
            systems.AddComponent<ProjectilePool>();
            systems.AddComponent<DebugOverlay>();

            AbilityDefinition abilityDefinition = LoadAbility();
            GameObject player = CreatePlayer(abilityDefinition);
            CreateCamera(player.transform);
            CreateLighting();

            EncounterDefinition[] encounters = LoadEncounters();
            EnemyDefinition[] enemies = LoadEnemies();

            EncounterDirector director = systems.AddComponent<EncounterDirector>();
            director.Configure(encounters, enemies, player.transform);

            GameSession session = systems.AddComponent<GameSession>();
            Health playerHealth = player.GetComponent<Health>();
            session.Configure(playerHealth, director);

            HudController hud = systems.AddComponent<HudController>();
            hud.Configure(
                session,
                player.GetComponent<AbilityController>(),
                player.GetComponent<PlayerMotor>());

            bool benchmarkRequested = Environment.GetCommandLineArgs().Any(
                argument => argument.Equals(
                    "-polarity-benchmark",
                    StringComparison.OrdinalIgnoreCase));
            if (benchmarkRequested && FindFirstObjectByType<PerformanceProbe>() == null)
            {
                systems.AddComponent<PerformanceProbe>();
            }

            bool captureRequested = Environment.GetCommandLineArgs().Any(
                argument => argument.Equals(
                    "-polarity-capture",
                    StringComparison.OrdinalIgnoreCase));
            if (captureRequested)
            {
                systems.AddComponent<VisualCaptureProbe>();
            }
        }

        private void CreateArena()
        {
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Arena Floor",
                arenaRoot,
                new Vector3(0f, -0.5f, 0f),
                new Vector3(36f, 1f, 32f),
                new Color(0.055f, 0.09f, 0.115f));

            for (int x = -16; x <= 16; x += 4)
            {
                RuntimeArt.Primitive(
                    PrimitiveType.Cube,
                    $"Grid X {x}",
                    arenaRoot,
                    new Vector3(x, 0.015f, 0f),
                    new Vector3(0.025f, 0.02f, 31f),
                    new Color(0.12f, 0.28f, 0.32f),
                    false,
                    0.4f);
            }

            for (int z = -14; z <= 14; z += 4)
            {
                RuntimeArt.Primitive(
                    PrimitiveType.Cube,
                    $"Grid Z {z}",
                    arenaRoot,
                    new Vector3(0f, 0.016f, z),
                    new Vector3(35f, 0.02f, 0.025f),
                    new Color(0.12f, 0.28f, 0.32f),
                    false,
                    0.4f);
            }

            CreateWall("North Wall", new Vector3(0f, 2f, 16f), new Vector3(36f, 4f, 0.7f));
            CreateWall("South Wall", new Vector3(0f, 2f, -16f), new Vector3(36f, 4f, 0.7f));
            CreateWall("East Wall", new Vector3(18f, 2f, 0f), new Vector3(0.7f, 4f, 32f));
            CreateWall("West Wall", new Vector3(-18f, 2f, 0f), new Vector3(0.7f, 4f, 32f));
            CreateArenaAccents();

            CreateHazard(new Vector3(-10.5f, 0f, 2.5f), new Vector3(5.5f, 0.35f, 5.5f));
            CreateHazard(new Vector3(10.5f, 0f, 8f), new Vector3(4.2f, 0.35f, 4.2f));

            CreateCover(new Vector3(-4.5f, 1.1f, 4.5f), new Vector3(2f, 2.2f, 4.8f));
            CreateCover(new Vector3(5.5f, 1.1f, -1.5f), new Vector3(2f, 2.2f, 5.2f));
            CreateCover(new Vector3(0f, 0.8f, 10f), new Vector3(5f, 1.6f, 1.5f));

            Vector3[] cratePositions =
            {
                new(-7f, 0.8f, -4f),
                new(-3f, 0.8f, 0f),
                new(4f, 0.8f, 6.5f),
                new(9f, 0.8f, -7f),
                new(13f, 0.8f, 1f),
                new(-14f, 0.8f, 9f)
            };
            for (int i = 0; i < cratePositions.Length; i++)
            {
                CreateMagneticCrate(cratePositions[i], i + 1);
            }

            RuntimeArt.Label(
                arenaRoot,
                "MAGNETIC CONTAINMENT RANGE // 07",
                new Vector3(0f, 0.04f, 14.4f),
                new Color(0.2f, 0.75f, 0.82f),
                58,
                0.065f).transform.eulerAngles = new Vector3(90f, 0f, 0f);
        }

        private static GameObject CreatePlayer(AbilityDefinition ability)
        {
            GameObject player = new("Player");
            player.transform.position = new Vector3(0f, 1.15f, -11.5f);
            player.transform.rotation = Quaternion.identity;

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2.2f;
            controller.radius = 0.48f;
            controller.center = new Vector3(0f, 1.1f, 0f);
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 52f;

            Health health = player.AddComponent<Health>();
            health.Configure(100f, CombatFaction.Player);
            player.AddComponent<DamageReceiver>();
            PlayerMotor motor = player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerCombat>();
            AbilityController abilities = player.AddComponent<AbilityController>();
            abilities.Configure(ability);

            Transform model = new GameObject("Player Model").transform;
            model.SetParent(player.transform, false);
            RuntimeArt.Primitive(
                PrimitiveType.Capsule,
                "Armoured Torso",
                model,
                new Vector3(0f, 1.12f, 0f),
                new Vector3(0.66f, 0.72f, 0.54f),
                new Color(0.48f, 0.62f, 0.7f),
                false,
                0.1f);
            RuntimeArt.Primitive(
                PrimitiveType.Sphere,
                "Helmet",
                model,
                new Vector3(0f, 1.83f, 0f),
                new Vector3(0.58f, 0.52f, 0.55f),
                new Color(0.62f, 0.76f, 0.82f),
                false,
                0.12f);
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Visor",
                model,
                new Vector3(0f, 1.84f, 0.49f),
                new Vector3(0.68f, 0.16f, 0.07f),
                RuntimeArt.Pull,
                false,
                2.4f);
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Hip Plate",
                model,
                new Vector3(0f, 0.58f, 0f),
                new Vector3(0.62f, 0.25f, 0.46f),
                RuntimeArt.Steel,
                false);

            for (int side = -1; side <= 1; side += 2)
            {
                RuntimeArt.Primitive(
                    PrimitiveType.Cylinder,
                    side < 0 ? "Left Leg" : "Right Leg",
                    model,
                    new Vector3(side * 0.23f, 0.27f, 0f),
                    new Vector3(0.18f, 0.35f, 0.18f),
                    new Color(0.32f, 0.46f, 0.54f),
                    false);
                RuntimeArt.Primitive(
                    PrimitiveType.Cube,
                    side < 0 ? "Left Boot" : "Right Boot",
                    model,
                    new Vector3(side * 0.23f, 0.06f, 0.11f),
                    new Vector3(0.25f, 0.17f, 0.42f),
                    RuntimeArt.Slate,
                    false);
                RuntimeArt.Primitive(
                    PrimitiveType.Cube,
                    side < 0 ? "Left Shoulder" : "Right Shoulder",
                    model,
                    new Vector3(side * 0.53f, 1.33f, 0f),
                    new Vector3(0.28f, 0.3f, 0.44f),
                    side < 0 ? RuntimeArt.Pull : RuntimeArt.Push,
                    false,
                    0.7f);
            }

            GameObject cannon = RuntimeArt.Primitive(
                PrimitiveType.Cylinder,
                "Anchor Projector",
                model,
                new Vector3(0.58f, 1.02f, 0.24f),
                new Vector3(0.18f, 0.34f, 0.18f),
                RuntimeArt.Push,
                false,
                1.3f);
            cannon.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

            GameObject fieldPack = RuntimeArt.Primitive(
                PrimitiveType.Cylinder,
                "Field Pack",
                model,
                new Vector3(0f, 1.12f, -0.53f),
                new Vector3(0.34f, 0.34f, 0.34f),
                RuntimeArt.Slate,
                false,
                0.2f);
            fieldPack.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

            GameObject fieldCoil = RuntimeArt.Primitive(
                PrimitiveType.Cylinder,
                "Rotating Field Coil",
                model,
                new Vector3(0f, 1.12f, -0.72f),
                new Vector3(0.47f, 0.045f, 0.47f),
                RuntimeArt.Pull,
                false,
                2.2f);
            fieldCoil.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Coil Index",
                fieldCoil.transform,
                new Vector3(0f, 0.2f, 0f),
                new Vector3(0.12f, 0.45f, 0.06f),
                RuntimeArt.Push,
                false,
                2f);

            TrailRenderer dashTrail = fieldPack.AddComponent<TrailRenderer>();
            dashTrail.time = 0.24f;
            dashTrail.minVertexDistance = 0.06f;
            dashTrail.startWidth = 0.75f;
            dashTrail.endWidth = 0.05f;
            dashTrail.numCornerVertices = 3;
            dashTrail.sharedMaterial = RuntimeArt.Material(RuntimeArt.Pull, 1f, true);
            dashTrail.startColor = new Color(RuntimeArt.Pull.r, RuntimeArt.Pull.g, RuntimeArt.Pull.b, 0.55f);
            dashTrail.endColor = new Color(RuntimeArt.Push.r, RuntimeArt.Push.g, RuntimeArt.Push.b, 0f);
            dashTrail.emitting = false;

            PlayerPresentation presentation = player.AddComponent<PlayerPresentation>();
            presentation.Configure(model, fieldCoil.transform);
            _ = motor;
            return player;
        }

        private static void CreateCamera(Transform player)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 64f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 180f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RuntimeArt.Dark;
            cameraObject.AddComponent<AudioListener>();
            CameraRig rig = cameraObject.AddComponent<CameraRig>();
            rig.Follow(player);
        }

        private static void CreateLighting()
        {
            GameObject sunObject = new("Arena Key Light");
            sunObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.72f, 0.84f, 1f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;

            Vector3[] points =
            {
                new(-12f, 6f, -10f),
                new(12f, 6f, -8f),
                new(-10f, 6f, 10f),
                new(10f, 6f, 11f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                GameObject lightObject = new($"Arena Light {i + 1:00}");
                lightObject.transform.position = points[i];
                Light point = lightObject.AddComponent<Light>();
                point.type = LightType.Point;
                point.range = 18f;
                point.intensity = 3.2f;
                point.color = i % 2 == 0 ? RuntimeArt.Pull : RuntimeArt.Push;
            }
        }

        private void CreateWall(string wallName, Vector3 position, Vector3 scale)
        {
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                wallName,
                arenaRoot,
                position,
                scale,
                new Color(0.035f, 0.065f, 0.09f));
        }

        private void CreateArenaAccents()
        {
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "North Cyan Rail",
                arenaRoot,
                new Vector3(-8.7f, 3.35f, 15.58f),
                new Vector3(17f, 0.08f, 0.09f),
                RuntimeArt.Pull,
                false,
                2.4f);
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "North Red Rail",
                arenaRoot,
                new Vector3(8.7f, 3.35f, 15.58f),
                new Vector3(17f, 0.08f, 0.09f),
                RuntimeArt.Push,
                false,
                2.4f);
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "South Cyan Rail",
                arenaRoot,
                new Vector3(8.7f, 3.35f, -15.58f),
                new Vector3(17f, 0.08f, 0.09f),
                RuntimeArt.Pull,
                false,
                2.4f);
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "South Red Rail",
                arenaRoot,
                new Vector3(-8.7f, 3.35f, -15.58f),
                new Vector3(17f, 0.08f, 0.09f),
                RuntimeArt.Push,
                false,
                2.4f);

            Transform emblem = new GameObject("Arena Field Emblem").transform;
            emblem.SetParent(arenaRoot, false);
            RuntimeArt.Ring(emblem, 5.4f, new Color(RuntimeArt.Pull.r, RuntimeArt.Pull.g, RuntimeArt.Pull.b, 0.3f), 0.035f, 80);
            RuntimeArt.Ring(emblem, 6.2f, new Color(RuntimeArt.Push.r, RuntimeArt.Push.g, RuntimeArt.Push.b, 0.2f), 0.018f, 80);

            Vector3[] beaconPositions =
            {
                new(-16.4f, 0f, -13.6f),
                new(16.4f, 0f, -13.6f),
                new(-16.4f, 0f, 13.6f),
                new(16.4f, 0f, 13.6f)
            };

            for (int i = 0; i < beaconPositions.Length; i++)
            {
                Transform beacon = new GameObject($"Boundary Beacon {i + 1:00}").transform;
                beacon.SetParent(arenaRoot, false);
                beacon.localPosition = beaconPositions[i];
                RuntimeArt.Primitive(
                    PrimitiveType.Cylinder,
                    "Beacon Base",
                    beacon,
                    new Vector3(0f, 0.55f, 0f),
                    new Vector3(0.42f, 0.55f, 0.42f),
                    RuntimeArt.Slate,
                    false);
                RuntimeArt.Primitive(
                    PrimitiveType.Cube,
                    "Beacon Light",
                    beacon,
                    new Vector3(0f, 1.25f, 0f),
                    new Vector3(0.24f, 0.55f, 0.24f),
                    i % 2 == 0 ? RuntimeArt.Pull : RuntimeArt.Push,
                    false,
                    2.6f);
            }
        }

        private void CreateCover(Vector3 position, Vector3 scale)
        {
            GameObject cover = RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Magnetic Cover",
                arenaRoot,
                position,
                scale,
                new Color(0.08f, 0.15f, 0.18f));
            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Cover Light",
                cover.transform,
                new Vector3(0f, 0.45f, -0.51f),
                new Vector3(0.72f, 0.08f, 0.03f),
                RuntimeArt.Pull,
                false,
                2f);
        }

        private void CreateHazard(Vector3 position, Vector3 scale)
        {
            GameObject hazardRoot = new("Plasma Hazard");
            hazardRoot.transform.SetParent(arenaRoot, false);
            hazardRoot.transform.localPosition = position;
            hazardRoot.AddComponent<HazardPulse>();

            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Plasma Core",
                hazardRoot.transform,
                new Vector3(0f, 0.08f, 0f),
                scale,
                RuntimeArt.Push,
                false,
                2.5f);

            BoxCollider trigger = hazardRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.75f, 0f);
            trigger.size = new Vector3(scale.x, 1.7f, scale.z);
            hazardRoot.AddComponent<Hazard>();

            for (int i = -2; i <= 2; i++)
            {
                RuntimeArt.Primitive(
                    PrimitiveType.Cube,
                    $"Hazard Rail {i + 3}",
                    hazardRoot.transform,
                    new Vector3(i * scale.x * 0.18f, 0.25f, 0f),
                    new Vector3(0.05f, 0.4f, scale.z * 0.92f),
                    RuntimeArt.Gold,
                    false,
                    1.4f);
            }
        }

        private void CreateMagneticCrate(Vector3 position, int index)
        {
            GameObject crate = RuntimeArt.Primitive(
                PrimitiveType.Cube,
                $"Magnetic Crate {index:00}",
                arenaRoot,
                position,
                Vector3.one * 1.35f,
                new Color(0.18f, 0.36f, 0.4f),
                true,
                0.3f);
            Rigidbody body = crate.AddComponent<Rigidbody>();
            body.mass = 2.4f;
            body.linearDamping = 0.55f;
            body.angularDamping = 0.8f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            MagneticTarget target = crate.AddComponent<MagneticTarget>();
            target.Configure(MagneticPolarity.Positive, 1.2f);

            RuntimeArt.Primitive(
                PrimitiveType.Cube,
                "Polarity Mark",
                crate.transform,
                new Vector3(0f, 0f, 0.51f),
                new Vector3(0.5f, 0.12f, 0.03f),
                RuntimeArt.Pull,
                false,
                2f);
        }

        private static AbilityDefinition LoadAbility()
        {
            AbilityDefinition definition = Resources.Load<AbilityDefinition>("Data/MagneticAnchorAbility");
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<AbilityDefinition>();
            definition.ConfigureDemoDefaults();
            return definition;
        }

        private static EnemyDefinition[] LoadEnemies()
        {
            EnemyDefinition[] definitions = Resources.LoadAll<EnemyDefinition>("Data/Enemies");
            if (definitions.Length >= 3)
            {
                return definitions;
            }

            return new[]
            {
                CreateEnemyFallback(EnemyArchetype.Chaser, 65f, 6.2f, 2.3f, 18f, 1.7f, RuntimeArt.Push),
                CreateEnemyFallback(EnemyArchetype.Shooter, 48f, 4.6f, 18f, 14f, 2.2f, new Color(0.72f, 0.35f, 1f)),
                CreateEnemyFallback(EnemyArchetype.Shield, 105f, 4.2f, 2.6f, 24f, 2.3f, RuntimeArt.Gold)
            };
        }

        private static EnemyDefinition CreateEnemyFallback(
            EnemyArchetype kind,
            float health,
            float speed,
            float range,
            float damage,
            float cooldown,
            Color accent)
        {
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.Configure(kind, health, speed, range, damage, cooldown, accent);
            return definition;
        }

        private static EncounterDefinition[] LoadEncounters()
        {
            EncounterDefinition[] definitions = Resources.LoadAll<EncounterDefinition>("Data/Encounters")
                .OrderBy(definition => definition.name, StringComparer.Ordinal)
                .ToArray();
            if (definitions.Length >= 3)
            {
                return definitions;
            }

            EncounterDefinition first = ScriptableObject.CreateInstance<EncounterDefinition>();
            first.Configure(
                "ENCOUNTER 01 // LEARN THE FORCE",
                "PULL a chaser off course. PUSH it into the plasma hazard.",
                new[]
                {
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(-7f, 1f, 3f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(7f, 1f, 5f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(0f, 1f, 11f))
                });

            EncounterDefinition second = ScriptableObject.CreateInstance<EncounterDefinition>();
            second.Configure(
                "ENCOUNTER 02 // REDIRECT",
                "Curve violet projectiles through an anchor field and return them to a robot.",
                new[]
                {
                    new EncounterDefinition.Spawn(EnemyArchetype.Shooter, new Vector3(-12f, 1f, 9f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Shooter, new Vector3(14f, 1f, 4f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(1f, 1f, 8f))
                });

            EncounterDefinition third = ScriptableObject.CreateInstance<EncounterDefinition>();
            third.Configure(
                "ENCOUNTER 03 // COMBINE",
                "Displace the shield unit, redirect fire, and control the whole arena.",
                new[]
                {
                    new EncounterDefinition.Spawn(EnemyArchetype.Shield, new Vector3(0f, 1f, 10f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Shooter, new Vector3(-14f, 1f, 7f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Shooter, new Vector3(13f, 1f, 4f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(-5f, 1f, 7f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(6f, 1f, 10f))
                });

            return new[] { first, second, third };
        }
    }
}
