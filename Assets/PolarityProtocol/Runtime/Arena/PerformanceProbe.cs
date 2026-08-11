using System;
using System.Collections;
using System.Linq;
using PolarityProtocol.Combat;
using PolarityProtocol.Data;
using PolarityProtocol.Encounters;
using PolarityProtocol.Magnetics;
using PolarityProtocol.Utilities;
using Unity.Profiling;
using UnityEngine;

namespace PolarityProtocol.Arena
{
    public sealed class PerformanceProbe : MonoBehaviour
    {
        private const float BenchmarkDuration = 15f;
        [SerializeField] private bool forceBenchmark;

        private ProfilerRecorder mainThreadRecorder;
        private ProfilerRecorder gcAllocationRecorder;
        private ProfilerRecorder drawCallRecorder;
        private ProfilerRecorder memoryRecorder;
        private float elapsed;
        private float deltaTotal;
        private float worstDelta;
        private int frames;
        private int initialCollectionCount;
        private int peakEnemies;
        private int peakProjectiles;
        private float nextCountSample;
        private bool running;

        private void Start()
        {
            bool requested = forceBenchmark ||
                             Environment.GetCommandLineArgs().Any(
                                 argument => argument.Equals(
                                     "-polarity-benchmark",
                                     StringComparison.OrdinalIgnoreCase));
            if (requested)
            {
                StartCoroutine(BeginWhenReady());
            }
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;
            elapsed += delta;
            deltaTotal += delta;
            worstDelta = Mathf.Max(worstDelta, delta);
            frames++;
            if (elapsed >= nextCountSample)
            {
                nextCountSample = elapsed + 0.5f;
                peakEnemies = Mathf.Max(
                    peakEnemies,
                    AI.EnemyBrain.ActiveCount);
                peakProjectiles = Mathf.Max(
                    peakProjectiles,
                    ProjectilePool.Active == null ? 0 : ProjectilePool.Active.ActiveCount);
            }

            if (elapsed >= BenchmarkDuration)
            {
                FinishBenchmark();
            }
        }

        public void ConfigureForStressTest()
        {
            forceBenchmark = true;
        }

        private IEnumerator BeginWhenReady()
        {
            while (GameSession.Active == null || ProjectilePool.Active == null)
            {
                yield return null;
            }

            GameSession.Active.BeginRun();
            GameSession.Active.PlayerHealth.Configure(100000f, CombatFaction.Player);
            SpawnStressLoad(GameSession.Active.PlayerHealth.transform);

            mainThreadRecorder = StartRecorder(ProfilerCategory.Internal, "Main Thread");
            gcAllocationRecorder = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
            drawCallRecorder = StartRecorder(ProfilerCategory.Render, "Draw Calls Count");
            memoryRecorder = StartRecorder(ProfilerCategory.Memory, "Total Used Memory");
            initialCollectionCount = GC.CollectionCount(0);
            running = true;

            Debug.Log(
                "[PolarityBenchmark] Started 15 second stress run with " +
                "18 additional enemies and 120 pooled projectiles.");
        }

        private static void SpawnStressLoad(Transform player)
        {
            EnemyDefinition[] definitions = Resources.LoadAll<EnemyDefinition>("Data/Enemies");
            for (int i = 0; i < 18; i++)
            {
                float angle = i * Mathf.PI * 2f / 18f;
                Vector3 position = new(
                    Mathf.Sin(angle) * (10f + i % 3),
                    1f,
                    Mathf.Cos(angle) * (10f + i % 3));
                EnemyArchetype kind = (EnemyArchetype)(i % 3);
                EnemyDefinition definition = definitions.FirstOrDefault(item => item.Archetype == kind);
                if (definition != null)
                {
                    EnemyFactory.Create(definition, player, position);
                }
            }

            for (int i = 0; i < 120; i++)
            {
                float angle = i * Mathf.PI * 2f / 120f;
                Vector3 position = new(
                    Mathf.Sin(angle) * 7f,
                    1.5f + (i % 5) * 0.32f,
                    Mathf.Cos(angle) * 7f);
                Vector3 tangent = new(Mathf.Cos(angle), 0.05f, -Mathf.Sin(angle));
                MagneticPolarity polarity = i % 2 == 0
                    ? MagneticPolarity.Negative
                    : MagneticPolarity.Positive;
                Color projectileColor = polarity == MagneticPolarity.Negative
                    ? RuntimeArt.Pull
                    : RuntimeArt.Push;
                ProjectilePool.Active.Spawn(
                    CombatFaction.Enemy,
                    null,
                    position,
                    tangent * (9f + i % 4),
                    1f,
                    projectileColor,
                    polarity);
            }
        }

        private void FinishBenchmark()
        {
            running = false;
            float averageDelta = frames > 0 ? deltaTotal / frames : 0f;
            long mainThreadNanoseconds = LastValue(mainThreadRecorder);
            long allocatedBytes = LastValue(gcAllocationRecorder);
            long drawCalls = LastValue(drawCallRecorder);
            long usedMemory = LastValue(memoryRecorder);
            int genZeroCollections = GC.CollectionCount(0) - initialCollectionCount;

            Debug.Log(
                $"[PolarityBenchmark] Result frames={frames} " +
                $"avgFrameMs={averageDelta * 1000f:0.00} " +
                $"worstFrameMs={worstDelta * 1000f:0.00} " +
                $"avgFps={(averageDelta > 0f ? 1f / averageDelta : 0f):0.0} " +
                $"mainThreadMs={mainThreadNanoseconds / 1000000f:0.00} " +
                $"gcAllocatedLastFrameKB={allocatedBytes / 1024f:0.00} " +
                $"gen0Collections={genZeroCollections} " +
                $"drawCalls={drawCalls} " +
                $"usedMemoryMB={usedMemory / (1024f * 1024f):0.0} " +
                $"peakEnemies={peakEnemies} " +
                $"peakProjectiles={peakProjectiles}");

            mainThreadRecorder.Dispose();
            gcAllocationRecorder.Dispose();
            drawCallRecorder.Dispose();
            memoryRecorder.Dispose();
            Application.Quit(0);
        }

        private static ProfilerRecorder StartRecorder(ProfilerCategory category, string marker)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, marker, 32);
            }
            catch
            {
                return default;
            }
        }

        private static long LastValue(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : 0L;
        }
    }
}
