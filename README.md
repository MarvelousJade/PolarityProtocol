# Polarity Protocol

Polarity Protocol is a compact third-person Unity combat demo where two magnetic anchor polarities pull or push robots, physics props, and hostile projectiles through a neon containment arena.

Built with Unity `6000.4.11f1` and C#. The playable Windows development build is generated at `Builds/Windows/PolarityProtocol.exe`.

## Gameplay at a glance

- Place up to two persistent magnetic anchors on arena surfaces.
- Opposite polarities **pull** while matching polarities **push**.
- Pull blue negative enemies with a red positive anchor, then repel them with blue.
- Redirect shooter projectiles by bending them through a player-owned field.
- Displace shield units to expose them for three seconds.
- Push enemies into pulsing plasma hazards.
- Clear three escalating encounters and earn a score based on time, damage, and redirections.

The demo has a responsive Unity UI Toolkit intro/tutorial, combat HUD, pause/restart flow, failure recovery, completion scoring, camera shake, hit stop, synthesized feedback audio, and an IMGUI runtime diagnostics mode.

## Controls

| Action | Keyboard and mouse | Xbox-style controller |
|---|---|---|
| Move | `WASD` | Left stick |
| Aim / orbit | Mouse | Right stick |
| Fire | Left mouse | `RB` or `A` |
| Place anchor | Right mouse | `LB` |
| Toggle blue/red polarity | `Q` | `X` |
| Recall anchors | `R` | `Y` |
| Dash | `Space` | `B` |
| Sprint | `Shift` | Left stick press |
| Pause | `Esc` | Menu |
| Navigate menus | Arrow keys / `W` / `S` | Left stick |
| Select | `Enter` / `Space` | `A` |
| Diagnostics | `F3` | Keyboard only |

## Technical features

- Analytic, unit-tested magnetic force solver with distance falloff and minimum-distance stabilization
- Non-allocating `Physics.OverlapSphereNonAlloc` field queries
- Data-driven ability, enemy, and encounter definitions using ScriptableObjects
- Three enemy decision models: chaser, ranged spacing, and directional shield
- Standardized damage messages and faction-aware redirectable projectiles
- Generic component pool used by the projectile pool
- Procedurally assembled arena and presentation; no marketplace gameplay packages
- Camera-relative movement, orbit/collision camera, dash, mouse, keyboard, and controller input
- Responsive runtime HUD, tutorial, pause, failure, and results interfaces built with reusable Unity UI Toolkit UXML/USS components
- Focused keyboard/controller menu navigation and C# state/event wiring without a third-party UI dependency
- Runtime force vectors, AI states, targeting ranges, active counts, and frame-rate diagnostics retained as an IMGUI developer overlay
- Encounter authoring window with draggable spawn handles, range preview, and bounds/separation validation
- Dedicated stress scene and packaged-player benchmark mode
- Twelve Edit Mode tests and ten Play Mode tests

## Project layout

```text
Assets/PolarityProtocol/
├── Editor/                    # Project builder and encounter authoring tool
├── Resources/
│   ├── Data/                  # Generated ScriptableObject tuning assets
│   └── UI/                    # Runtime UI Toolkit UXML, USS, and theme
├── Runtime/
│   ├── Abilities/
│   ├── AI/
│   ├── Arena/
│   ├── Combat/
│   ├── Data/
│   ├── Encounters/
│   ├── Magnetics/
│   ├── Player/
│   ├── Pooling/
│   ├── UI/
│   └── Utilities/
├── Scenes/
│   ├── PolarityProtocolDemo.unity
│   └── PolarityProtocolStress.unity
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

See [Architecture](docs/ARCHITECTURE.md) for system boundaries and data flow.

## Open and play

1. Open the repository folder in Unity Hub with Unity `6000.4.11f1`.
2. Open `Assets/PolarityProtocol/Scenes/PolarityProtocolDemo.unity`.
3. Press Play.

The checked-in scene is intentionally small. `ArenaBootstrap` builds the authored graybox arena and connects runtime systems, while persistent tuning lives in ScriptableObject assets.

## Rebuild content and executable

Inside Unity:

1. Use **Tools → Polarity Protocol → Build Demo Project** to regenerate data and both scenes.
2. Use **Tools → Polarity Protocol → Build Windows Player** to create the executable.
3. Use **Tools → Polarity Protocol → Build WebGL Player** to create a browser build for itch.io.
4. Use **Tools → Polarity Protocol → Encounter Authoring** to edit and validate encounter layouts.

For CI or a local PowerShell terminal:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'G:\C++ PROJECTS\PolarityProtocol' `
  -executeMethod PolarityProtocol.Editor.ProjectBuilder.BuildWindowsPlayer `
  -logFile 'G:\C++ PROJECTS\PolarityProtocol\Logs\windows-build.log'
```

For a WebGL build:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'G:\C++ PROJECTS\PolarityProtocol' `
  -executeMethod PolarityProtocol.Editor.ProjectBuilder.BuildWebGLPlayer `
  -logFile 'G:\C++ PROJECTS\PolarityProtocol\Logs\webgl-build.log'
```

The WebGL builder produces a release player in `Builds/WebGL` with gzip compression and a decompression fallback for static hosts. Zip the directory contents—not the enclosing directory—so `index.html` is at the archive root:

```powershell
Compress-Archive -Path '.\Builds\WebGL\*' `
  -DestinationPath '.\Builds\PolarityProtocol-WebGL.zip' -Force
```

On itch.io, upload the zip as an HTML project and enable **This file will be played in the browser**. Build output is ignored by Git because Unity players are large and fully reproducible.

## Tests

Run the suites from Unity Test Runner, or use:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' `
  -batchmode -projectPath 'G:\C++ PROJECTS\PolarityProtocol' `
  -runTests -testPlatform EditMode `
  -testResults 'G:\C++ PROJECTS\PolarityProtocol\Logs\editmode-results.xml'

& 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' `
  -batchmode -projectPath 'G:\C++ PROJECTS\PolarityProtocol' `
  -runTests -testPlatform PlayMode `
  -testResults 'G:\C++ PROJECTS\PolarityProtocol\Logs\playmode-results.xml'
```

Verified locally:

- Edit Mode: **12/12 passed**
- Play Mode: **10/10 passed**
- Windows x64 development player: **build succeeded**
- Packaged-player smoke run: **no runtime exceptions**

## Performance

The reproducible stress workload adds 18 robots and 120 projectiles to the opening encounter. On an AMD Ryzen 5 3600, Radeon RX 5700, and 16 GB RAM at 1280×720:

- 21 peak enemies
- 120 peak projectiles
- 106.7 average FPS / 9.38 ms average frame in the conservative UI Toolkit feature run
- 8.32 ms sampled main-thread time in both latest feature runs
- 151.0 MB sampled used memory in the UI Toolkit feature run
- Gen-0 collections reduced from 17 to 2 after pooled feedback and cached UI/diagnostic update paths

Full methodology and caveats are in [Performance](docs/PERFORMANCE.md).

## Known limitations

- Presentation is intentionally procedural: primitive models, generated materials, simple robot motion, and synthesized tones.
- Enemy navigation uses local steering and arena collision rather than a baked NavMesh.
- The legacy input map targets Xbox-style Windows controllers; unusual controller layouts may need axis remapping.
- The development build prioritizes inspection over distribution size and is approximately 141.6 MiB.
- No gameplay video or GIF is checked in yet.
- The batch benchmark could not retrieve the draw-call counter on this Unity/player configuration; the report marks it unavailable rather than presenting zero as a real count.

## Credits, licensing, and contribution scope

All gameplay code, tests, editor tooling, procedural geometry, materials, UI, and synthesized audio in this repository were created specifically for this demo. No external art, audio, animation, or gameplay packages are included. Unity primitive meshes, built-in shaders, default font resources, engine binaries, and the Unity Test Framework remain subject to their respective Unity terms.

See [Third-party notices](THIRD_PARTY_NOTICES.md).

