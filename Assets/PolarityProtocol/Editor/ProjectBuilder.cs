using System;
using System.IO;
using System.IO.Compression;
using PolarityProtocol.Arena;
using PolarityProtocol.Data;
using PolarityProtocol.Utilities;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolarityProtocol.Editor
{
    public static class ProjectBuilder
    {
        public const string DemoScenePath = "Assets/PolarityProtocol/Scenes/PolarityProtocolDemo.unity";
        public const string StressScenePath = "Assets/PolarityProtocol/Scenes/PolarityProtocolStress.unity";
        private const string DataRoot = "Assets/PolarityProtocol/Resources/Data";

        [MenuItem("Tools/Polarity Protocol/Build Demo Project", priority = 1)]
        public static void BuildDemoProject()
        {
            EnsureFolders();
            GenerateAbility();
            GenerateEnemies();
            GenerateEncounters();
            GenerateScene();
            ConfigureProject();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Polarity Protocol] Demo scene and data assets generated successfully.");
        }

        [MenuItem("Tools/Polarity Protocol/Build Windows Player", priority = 2)]
        public static void BuildWindowsPlayer()
        {
            BuildDemoProject();

            string buildDirectory = Path.GetFullPath("Builds/Windows");
            Directory.CreateDirectory(buildDirectory);
            string executablePath = Path.Combine(buildDirectory, "PolarityProtocol.exe");

            BuildPlayerOptions options = new()
            {
                scenes = new[] { DemoScenePath },
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Polarity Protocol Windows build failed: {report.summary.result} " +
                    $"({report.summary.totalErrors} errors).");
            }

            Debug.Log(
                $"[Polarity Protocol] Windows player built at {executablePath} " +
                $"({report.summary.totalSize / (1024f * 1024f):0.0} MiB).");
        }

        [MenuItem("Tools/Polarity Protocol/Build WebGL Player", priority = 3)]
        public static void BuildWebGLPlayer()
        {
            BuildDemoProject();

            string buildDirectory = Path.GetFullPath("Builds/WebGL");
            Directory.CreateDirectory(buildDirectory);

            BuildPlayerOptions options = new()
            {
                scenes = new[] { DemoScenePath },
                locationPathName = buildDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Polarity Protocol WebGL build failed: {report.summary.result} " +
                    $"({report.summary.totalErrors} errors).");
            }

            string archivePath = CreateWebGLArchive(buildDirectory);
            Debug.Log(
                $"[Polarity Protocol] WebGL player built at {buildDirectory} " +
                $"({report.summary.totalSize / (1024f * 1024f):0.0} MiB). " +
                $"Itch.io upload archive created at {archivePath}.");
        }

        private static string CreateWebGLArchive(string buildDirectory)
        {
            string archivePath = Path.GetFullPath("Builds/PolarityProtocol-WebGL.zip");
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            string normalizedRoot = Path.GetFullPath(buildDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string[] files = Directory.GetFiles(
                normalizedRoot,
                "*",
                SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);

            using FileStream archiveStream = File.Create(archivePath);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.Create);
            for (int i = 0; i < files.Length; i++)
            {
                string entryPath = files[i]
                    .Substring(normalizedRoot.Length)
                    .Replace('\\', '/');
                ZipArchiveEntry entry = archive.CreateEntry(
                    entryPath,
                    System.IO.Compression.CompressionLevel.Optimal);
                using Stream source = File.OpenRead(files[i]);
                using Stream destination = entry.Open();
                source.CopyTo(destination);
            }

            return archivePath;
        }

        public static void BuildAll()
        {
            BuildWindowsPlayer();
        }

        public static void OpenDemoScene()
        {
            if (!File.Exists(DemoScenePath))
            {
                BuildDemoProject();
            }

            EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        }

        private static void GenerateAbility()
        {
            AbilityDefinition ability = LoadOrCreate<AbilityDefinition>(
                $"{DataRoot}/MagneticAnchorAbility.asset");
            ability.ConfigureDemoDefaults();
            EditorUtility.SetDirty(ability);
        }

        private static void GenerateEnemies()
        {
            CreateEnemy(
                "Chaser",
                EnemyArchetype.Chaser,
                65f,
                6.2f,
                2.3f,
                18f,
                1.7f,
                RuntimeArt.Push);
            CreateEnemy(
                "Shooter",
                EnemyArchetype.Shooter,
                48f,
                4.6f,
                18f,
                14f,
                2.2f,
                new Color(0.72f, 0.35f, 1f));
            CreateEnemy(
                "Shield",
                EnemyArchetype.Shield,
                105f,
                4.2f,
                2.6f,
                24f,
                2.3f,
                RuntimeArt.Gold);
        }

        private static void CreateEnemy(
            string assetName,
            EnemyArchetype kind,
            float health,
            float speed,
            float range,
            float damage,
            float cooldown,
            Color accent)
        {
            EnemyDefinition enemy = LoadOrCreate<EnemyDefinition>(
                $"{DataRoot}/Enemies/{assetName}.asset");
            enemy.Configure(kind, health, speed, range, damage, cooldown, accent);
            EditorUtility.SetDirty(enemy);
        }

        private static void GenerateEncounters()
        {
            CreateEncounter(
                "01_LearnTheForce",
                "ENCOUNTER 01 // LEARN THE FORCE",
                "Use the OPPOSITE colour to PULL robots. MATCH their colour to PUSH them into plasma.",
                new[]
                {
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(-7f, 1f, 3f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(7f, 1f, 5f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(0f, 1f, 11f))
                });

            CreateEncounter(
                "02_Redirect",
                "ENCOUNTER 02 // REDIRECT",
                "Use the opposite-colour anchor to curve red and blue projectiles back at a robot.",
                new[]
                {
                    new EncounterDefinition.Spawn(EnemyArchetype.Shooter, new Vector3(-12f, 1f, 9f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Shooter, new Vector3(14f, 1f, 4f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(1f, 1f, 8f))
                });

            CreateEncounter(
                "03_Combine",
                "ENCOUNTER 03 // COMBINE",
                "Match the robot to pull off its opposite-colour shield, then switch colour to pull the body.",
                new[]
                {
                    new EncounterDefinition.Spawn(EnemyArchetype.Shield, new Vector3(0f, 1f, 10f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Shooter, new Vector3(-14f, 1f, 7f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Shooter, new Vector3(13f, 1f, 4f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(-5f, 1f, 7f)),
                    new EncounterDefinition.Spawn(EnemyArchetype.Chaser, new Vector3(6f, 1f, 10f))
                });
        }

        private static void CreateEncounter(
            string assetName,
            string displayName,
            string objective,
            EncounterDefinition.Spawn[] spawns)
        {
            EncounterDefinition encounter = LoadOrCreate<EncounterDefinition>(
                $"{DataRoot}/Encounters/{assetName}.asset");
            encounter.Configure(displayName, objective, spawns);
            EditorUtility.SetDirty(encounter);
        }

        private static void GenerateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrap = new("Polarity Protocol");
            bootstrap.AddComponent<ArenaBootstrap>();
            EditorSceneManager.SaveScene(scene, DemoScenePath);

            Scene stressScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject stressBootstrap = new("Polarity Protocol Stress Test");
            stressBootstrap.AddComponent<ArenaBootstrap>();
            PerformanceProbe probe = stressBootstrap.AddComponent<PerformanceProbe>();
            probe.ConfigureForStressTest();
            EditorSceneManager.SaveScene(stressScene, StressScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(DemoScenePath, true),
                new EditorBuildSettingsScene(StressScenePath, false)
            };

            EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            ArenaBootstrap reopenedBootstrap = UnityEngine.Object.FindFirstObjectByType<ArenaBootstrap>();
            Selection.activeGameObject = reopenedBootstrap == null ? null : reopenedBootstrap.gameObject;
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "Portfolio Systems Lab";
            PlayerSettings.productName = "Polarity Protocol";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.WebGL, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            EnsureAlwaysIncludedShaders("Standard", "Sprites/Default", "Legacy Shaders/Diffuse");
            AddInputAxis("Right Stick Horizontal", 3, false);
            AddInputAxis("Right Stick Vertical", 4, true);
        }

        private static void EnsureAlwaysIncludedShaders(params string[] shaderNames)
        {
            UnityEngine.Object[] graphicsAssets =
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsAssets.Length == 0)
            {
                Debug.LogWarning("Could not update the always-included shader list.");
                return;
            }

            SerializedObject graphicsSettings = new(graphicsAssets[0]);
            SerializedProperty includedShaders =
                graphicsSettings.FindProperty("m_AlwaysIncludedShaders");
            if (includedShaders == null)
            {
                Debug.LogWarning("GraphicsSettings does not expose m_AlwaysIncludedShaders.");
                return;
            }

            for (int shaderIndex = 0; shaderIndex < shaderNames.Length; shaderIndex++)
            {
                Shader shader = Shader.Find(shaderNames[shaderIndex]);
                if (shader == null)
                {
                    Debug.LogWarning($"Required shader '{shaderNames[shaderIndex]}' was not found.");
                    continue;
                }

                bool alreadyIncluded = false;
                for (int includedIndex = 0; includedIndex < includedShaders.arraySize; includedIndex++)
                {
                    if (includedShaders.GetArrayElementAtIndex(includedIndex).objectReferenceValue == shader)
                    {
                        alreadyIncluded = true;
                        break;
                    }
                }

                if (alreadyIncluded)
                {
                    continue;
                }

                includedShaders.InsertArrayElementAtIndex(includedShaders.arraySize);
                includedShaders.GetArrayElementAtIndex(includedShaders.arraySize - 1)
                    .objectReferenceValue = shader;
            }

            graphicsSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddInputAxis(string axisName, int axisNumber, bool invert)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
            if (assets.Length == 0)
            {
                Debug.LogWarning($"Could not configure legacy input axis '{axisName}'.");
                return;
            }

            SerializedObject inputManager = new(assets[0]);
            SerializedProperty axes = inputManager.FindProperty("m_Axes");
            for (int i = 0; i < axes.arraySize; i++)
            {
                if (axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue == axisName)
                {
                    return;
                }
            }

            axes.InsertArrayElementAtIndex(axes.arraySize);
            SerializedProperty axis = axes.GetArrayElementAtIndex(axes.arraySize - 1);
            axis.FindPropertyRelative("m_Name").stringValue = axisName;
            axis.FindPropertyRelative("descriptiveName").stringValue = string.Empty;
            axis.FindPropertyRelative("descriptiveNegativeName").stringValue = string.Empty;
            axis.FindPropertyRelative("negativeButton").stringValue = string.Empty;
            axis.FindPropertyRelative("positiveButton").stringValue = string.Empty;
            axis.FindPropertyRelative("altNegativeButton").stringValue = string.Empty;
            axis.FindPropertyRelative("altPositiveButton").stringValue = string.Empty;
            axis.FindPropertyRelative("gravity").floatValue = 0f;
            axis.FindPropertyRelative("dead").floatValue = 0.19f;
            axis.FindPropertyRelative("sensitivity").floatValue = 1f;
            axis.FindPropertyRelative("snap").boolValue = false;
            axis.FindPropertyRelative("invert").boolValue = invert;
            axis.FindPropertyRelative("type").intValue = 2;
            axis.FindPropertyRelative("axis").intValue = axisNumber;
            axis.FindPropertyRelative("joyNum").intValue = 0;
            inputManager.ApplyModifiedProperties();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/PolarityProtocol", "Scenes");
            EnsureFolder("Assets/PolarityProtocol", "Resources");
            EnsureFolder("Assets/PolarityProtocol/Resources", "Data");
            EnsureFolder(DataRoot, "Enemies");
            EnsureFolder(DataRoot, "Encounters");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string combined = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(combined))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
