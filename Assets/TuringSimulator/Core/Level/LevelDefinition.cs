using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;
using TuringSimulator.Core.Validation;
using UnityEngine;
using UnityEngine.Serialization;

namespace TuringSimulator.Core.Level
{
    [CreateAssetMenu(
        menuName = "Turing Simulator/Level",
        fileName = "LevelDefinition")]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Presentation")]
        [TextArea] public string title;
        [TextArea] public string description;
        
        [Header("Gameplay")]
        [Tooltip("Stable id for ITS/BKT and Python LEVEL_META (e.g. ReplaceAllWithNuts).")]
        public string levelId = "";

        public ValidationTest mainTest;
        public ValidationTest[] validationTests;

        public int ValidationScenarioCount =>
            (mainTest == null ? 0 : 1) + (validationTests?.Length ?? 0);
    }
}