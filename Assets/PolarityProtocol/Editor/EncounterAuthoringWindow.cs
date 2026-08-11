using PolarityProtocol.Data;
using PolarityProtocol.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PolarityProtocol.Editor
{
    public sealed class EncounterAuthoringWindow : EditorWindow
    {
        private EncounterDefinition selected;
        private Vector2 scroll;
        private string validationMessage = "Select an encounter, then validate its spawn layout.";
        private MessageType validationType = MessageType.Info;

        [MenuItem("Tools/Polarity Protocol/Encounter Authoring", priority = 20)]
        public static void Open()
        {
            GetWindow<EncounterAuthoringWindow>("Encounter Authoring");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawSceneHandles;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSceneHandles;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("POLARITY PROTOCOL", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Encounter authoring, range preview, and spawn validation",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate / Rebuild Demo", GUILayout.Height(32f)))
                {
                    ProjectBuilder.BuildDemoProject();
                }

                if (GUILayout.Button("Open Demo Scene", GUILayout.Height(32f)))
                {
                    ProjectBuilder.OpenDemoScene();
                }
            }

            EditorGUILayout.Space(10f);
            selected = (EncounterDefinition)EditorGUILayout.ObjectField(
                "Encounter",
                selected,
                typeof(EncounterDefinition),
                false);

            if (selected == null)
            {
                DrawEncounterPicker();
                EditorGUILayout.HelpBox(validationMessage, validationType);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            SerializedObject serializedEncounter = new(selected);
            serializedEncounter.Update();
            EditorGUILayout.PropertyField(serializedEncounter.FindProperty("displayName"));
            EditorGUILayout.PropertyField(serializedEncounter.FindProperty("objective"));
            EditorGUILayout.PropertyField(serializedEncounter.FindProperty("spawns"), true);
            serializedEncounter.ApplyModifiedProperties();

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("Validate Spawn Layout", GUILayout.Height(28f)))
            {
                ValidateSelected();
                SceneView.RepaintAll();
            }

            EditorGUILayout.HelpBox(validationMessage, validationType);
            EditorGUILayout.LabelField(
                "Scene handles",
                "Drag spawn markers in the Scene view. Cyan wire discs preview magnetic range.");
            EditorGUILayout.EndScrollView();
        }

        private void DrawEncounterPicker()
        {
            string[] guids = AssetDatabase.FindAssets("t:EncounterDefinition");
            if (guids.Length == 0)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Available encounters", EditorStyles.boldLabel);
            for (int i = 0; i < guids.Length; i++)
            {
                EncounterDefinition encounter = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (GUILayout.Button(encounter.DisplayName))
                {
                    selected = encounter;
                    ValidateSelected();
                    SceneView.RepaintAll();
                }
            }
        }

        private void DrawSceneHandles(SceneView _)
        {
            if (selected == null)
            {
                return;
            }

            AbilityDefinition ability = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(
                "Assets/PolarityProtocol/Resources/Data/MagneticAnchorAbility.asset");
            float radius = ability == null ? 7.5f : ability.Radius;
            EncounterDefinition.Spawn[] spawns = selected.Spawns;

            for (int i = 0; i < spawns.Length; i++)
            {
                EncounterDefinition.Spawn spawn = spawns[i];
                Color color = spawn.archetype switch
                {
                    EnemyArchetype.Chaser => RuntimeArt.Pull,
                    EnemyArchetype.Shooter => new Color(0.72f, 0.35f, 1f),
                    _ => RuntimeArt.Gold
                };

                Handles.color = color;
                Handles.DrawWireDisc(spawn.position, Vector3.up, radius);
                Handles.Label(
                    spawn.position + Vector3.up * 1.5f,
                    $"{i + 1:00}  {spawn.archetype.ToString().ToUpperInvariant()}");

                EditorGUI.BeginChangeCheck();
                Vector3 updated = Handles.PositionHandle(spawn.position, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selected, "Move encounter spawn");
                    spawn.position = updated;
                    spawns[i] = spawn;
                    EditorUtility.SetDirty(selected);
                    ValidateSelected();
                }
            }
        }

        private void ValidateSelected()
        {
            if (selected == null)
            {
                validationMessage = "No encounter selected.";
                validationType = MessageType.Warning;
                return;
            }

            EncounterDefinition.Spawn[] spawns = selected.Spawns;
            if (spawns.Length == 0)
            {
                validationMessage = "This encounter has no spawn points.";
                validationType = MessageType.Error;
                return;
            }

            int invalid = 0;
            int overlapping = 0;
            for (int i = 0; i < spawns.Length; i++)
            {
                Vector3 position = spawns[i].position;
                if (Mathf.Abs(position.x) > 17f || Mathf.Abs(position.z) > 15f || position.y < 0f)
                {
                    invalid++;
                }

                for (int j = i + 1; j < spawns.Length; j++)
                {
                    if (Vector3.Distance(position, spawns[j].position) < 1.5f)
                    {
                        overlapping++;
                    }
                }
            }

            if (invalid > 0)
            {
                validationMessage =
                    $"{invalid} spawn point(s) are outside the navigable arena bounds or below the floor.";
                validationType = MessageType.Error;
            }
            else if (overlapping > 0)
            {
                validationMessage =
                    $"{overlapping} spawn pair(s) are too close. Separate them by at least 1.5 metres.";
                validationType = MessageType.Warning;
            }
            else
            {
                validationMessage =
                    $"{spawns.Length} spawn point(s) valid. All are in bounds with safe separation.";
                validationType = MessageType.Info;
            }
        }
    }
}

