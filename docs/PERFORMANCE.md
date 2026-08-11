# Performance report

## Test environment

- Date: 2026-08-11 latest rerun (comparison runs: 2026-07-23)
- Unity: `6000.4.11f1`
- Build: Windows x64, Mono, Development Build
- Resolution: 1280×720
- Graphics API: Direct3D 12
- CPU: AMD Ryzen 5 3600, 6 cores
- GPU: AMD Radeon RX 5700, 8 GB VRAM
- Memory: 16 GB system RAM
- OS: Windows 11 Professional

## Reproducible workload

The normal demo includes `-polarity-benchmark` command-line handling, and the repository also contains `PolarityProtocolStress.unity`.

Benchmark mode:

- starts the normal opening encounter;
- adds 18 robots evenly split across chaser, shooter, and shield archetypes;
- launches 120 pooled, magnetic projectiles;
- keeps normal physics, AI, presentation, audio, and projectile updates active;
- samples for 15 seconds; and
- prints one `[PolarityBenchmark] Result` record before exiting.

Run it with:

```powershell
& '.\Builds\Windows\PolarityProtocol.exe' `
  -batchmode -polarity-benchmark `
  -screen-width 1280 -screen-height 720 `
  -logFile '.\Logs\benchmark.log'
```

## Results

| Measurement | Before optimization | After optimization | Hazard-aware AI |
|---|---:|---:|---:|
| Peak enemies | 21 | 21 | 21 |
| Peak projectiles | 120 | 120 | 120 |
| Average frame | 8.45 ms | 8.53 ms | 8.47 ms |
| Average FPS | 118.3 | 117.2 | 118.1 |
| Sampled main thread | 8.32 ms | 8.75 ms | 8.31 ms |
| Worst observed frame | 203.96 ms | 249.00 ms | 229.82 ms |
| Gen-0 collections | 17 | 4 | 4 |
| Last-frame GC allocation | 0.16 KB | 0.20 KB | 0.33 KB |
| Sampled used memory | 142.9 MB | 143.2 MB | 146.0 MB |

The average frame differences are within run-to-run variance. The meaningful optimization remains the Gen-0 collection count: **17 → 4**, a **76% reduction**, under the same entity and projectile load. The latest rerun includes expanded hazard steering, Rigidbody velocity look-ahead, and magnetic hazard-damage gating; it retained four Gen-0 collections and comparable frame time.

## Optimization

The initial implementation synthesized a new `AudioClip` and sample array for every feedback pulse. AI telegraphs and hits made that allocation path frequent under load. The runtime diagnostics also allocated enemy arrays while sampling.

The optimized implementation:

1. synthesizes a bank of 15 short tonal clips once in `FeedbackBus.Awake`;
2. maps each requested pulse to the nearest cached tone;
3. replays clips through `AudioSource.PlayOneShot`;
4. destroys the bank only when the feedback system is torn down; and
5. replaces benchmark enemy enumeration with `EnemyBrain.ActiveCount`.

Projectile materials and placement-preview materials were also converted from repeated construction to cached material updates before the second measurement.

## Existing safeguards

- Magnetic overlap uses a fixed 64-collider buffer.
- Projectile GameObjects are pooled and returned on impact or lifetime expiry.
- AI movement runs in `FixedUpdate`, while decisions avoid scene searches.
- Rigidbody references and materials are cached.
- Force debug lines are allocated lazily and only enabled with `F3`.
- Debug enemy enumeration occurs only while the diagnostics HUD is open.

## Interpretation and caveats

- This is a short development-build benchmark, not a shipping hardware certification.
- Worst-frame values include workload creation and shader/runtime warm-up; average and collection count are more representative of sustained behavior.
- The Unity `Draw Calls Count` recorder returned no value in this packaged batch configuration. It is therefore reported as unavailable, not as zero draw calls.
- Player input is not simulated during the benchmark, but the full AI, projectile, collision, magnetic-target, audio, rendering, and encounter loops remain active.
- A release build should be profiled separately before distribution because development instrumentation changes timing and size.
