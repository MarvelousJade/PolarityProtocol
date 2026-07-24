using System;
using UnityEngine;

namespace PolarityProtocol.Data
{
    [CreateAssetMenu(menuName = "Polarity Protocol/Encounter Definition", fileName = "EncounterDefinition")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        [Serializable]
        public struct Spawn
        {
            public EnemyArchetype archetype;
            public Vector3 position;

            public Spawn(EnemyArchetype kind, Vector3 spawnPosition)
            {
                archetype = kind;
                position = spawnPosition;
            }
        }

        [SerializeField] private string displayName = "Encounter";
        [SerializeField, TextArea] private string objective = "Defeat the robots.";
        [SerializeField] private Spawn[] spawns = Array.Empty<Spawn>();

        public string DisplayName => displayName;
        public string Objective => objective;
        public Spawn[] Spawns => spawns;

        public void Configure(string title, string instruction, Spawn[] entries)
        {
            displayName = title;
            objective = instruction;
            spawns = entries ?? Array.Empty<Spawn>();
        }
    }
}

