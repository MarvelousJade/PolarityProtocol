# Architecture

Polarity Protocol keeps configuration, simulation, orchestration, and presentation separate so individual systems can be tested or replaced without a global service locator.

## Runtime flow

```text
ScriptableObject data
  ├─ AbilityDefinition ──────> AbilityController ──> MagneticAnchor
  ├─ EnemyDefinition ────────> EncounterDirector ─> EnemyBrain
  └─ EncounterDefinition ────> EncounterDirector ─> EnemyFactory

MagneticAnchor
  └─ MagneticForceSolver ────> MagneticTarget ─────> Rigidbody
                                      ├────────────> EnemyBrain displacement
                                      └────────────> Projectile ownership redirect

PlayerCombat / EnemyBrain
  └─ ProjectilePool ─────────> Projectile ─────────> DamageReceiver ─> Health

GameSession
  ├─ owns run state, timing, damage, redirects, and score
  ├─ starts EncounterDirector
  └─ supplies read-only state to HudController
```

## Boundaries

### Data

`AbilityDefinition`, `EnemyDefinition`, and `EncounterDefinition` contain designer-tunable values. Runtime components consume these objects but do not mutate project assets.

### Magnetics

`MagneticForceSolver` is a pure function. It knows nothing about colliders, rigidbodies, enemies, or input. `MagneticAnchor` owns spatial queries and lifetime, while `MagneticTarget` adapts the calculated force to a Rigidbody and optional gameplay reactions.

The solver follows:

1. Reject targets outside the field radius.
2. Clamp distance to a stable minimum.
3. Apply exponential radial falloff.
4. Attract opposite polarities and repel matching polarities.
5. Return a force vector without changing scene state.

All demo targets have positive polarity, making negative anchors pull and positive anchors push.

### Combat

`DamageInfo` is the shared message format. `DamageReceiver` forwards it to `Health`, and optional `IDamageModifier` components can transform it. The shield enemy uses this seam to reduce frontal damage without coupling projectile code to enemy types.

`Projectile` owns collision, faction, lifetime, and redirection state. `ProjectilePool` owns reuse. Neither player nor AI code instantiates projectile GameObjects directly.

### AI

`EnemyBrain` owns perception and decisions but delegates health, physics, magnetics, projectiles, and presentation data. The three archetypes share safe state transitions:

- Chaser: pursue → telegraph → melee → recover
- Shooter: approach/retreat/strafe → telegraph → fire → recover
- Shield: chaser behavior plus a frontal damage modifier and magnetic exposure window

Local hazard steering expands each plasma trigger by the enemy collider clearance, routes inward movement around the nearest edge, redirects unsafe Rigidbody momentum, and corrects unsafe authored spawn positions. During magnetic displacement the AI motor yields, so field force—not pursuit locomotion—is what can carry a movable enemy into plasma. Enemy hazard damage is gated by a short recent-field window; players and other ungated health targets still take environmental damage normally.

Magnetic force never directly edits AI state internals; `MagneticTarget` reports displacement through `NotifyMagneticForce`.

### Encounters and session

`EncounterDirector` consumes ordered encounter assets, spawns through `EnemyFactory`, tracks living `Health` components, and advances after the current set is defeated.

`GameSession` is deliberately narrow. It owns only top-level run state, pause/restart, timer, score inputs, and completion/failure. It does not expose every gameplay subsystem.

### Presentation

`ArenaBootstrap` constructs the procedural arena and composition root. `RuntimeArt` is a small factory for primitive presentation. HUD, audio feedback, camera, and debug visuals read gameplay state without owning simulation rules.

## Allocation strategy

- Anchor queries use a fixed collider buffer and `Physics.OverlapSphereNonAlloc`.
- A per-step `HashSet<MagneticTarget>` prevents double forces from compound colliders.
- Projectiles use `ComponentPool<Projectile>`.
- Projectile, preview, enemy, and hazard materials are cached rather than recreated during updates.
- Feedback tones are synthesized once and replayed from a prewarmed clip bank.
- Expensive enemy enumeration is restricted to the opt-in diagnostics HUD; the benchmark uses `EnemyBrain.ActiveCount`.

## Content generation

`ProjectBuilder` is deterministic and safe to rerun. It:

1. Creates or updates ScriptableObject assets.
2. Generates demo and stress scenes.
3. configures controller axes and player settings.
4. registers the demo scene for builds.
5. optionally creates a Windows x64 development player.

The scene remains a minimal composition root. This avoids fragile hand-wired references while keeping tuning data inspectable and version-controlled.

## Designer tooling

The Encounter Authoring window supports:

- selecting generated encounter assets;
- editing wave names, objectives, archetypes, and spawn positions;
- dragging spawn points in Scene view;
- magnetic range preview around every spawn;
- arena-bound warnings;
- below-floor warnings; and
- minimum spawn separation validation.
