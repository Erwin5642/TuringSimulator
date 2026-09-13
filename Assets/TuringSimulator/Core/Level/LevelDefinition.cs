using TuringSimulator.Core.Validation;
using UnityEngine;

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
        [Tooltip("Stable id for ITS/BKT and Python LEVEL_META (e.g. AppendScrew).")]
        public string levelId = "";

        public ValidationTest[] validationTests;

        public int ValidationScenarioCount => validationTests?.Length ?? 0;
    }
}
