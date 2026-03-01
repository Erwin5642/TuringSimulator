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
        public ValidationTest mainTest;
        public ValidationTest[] validationTests;
    }
}