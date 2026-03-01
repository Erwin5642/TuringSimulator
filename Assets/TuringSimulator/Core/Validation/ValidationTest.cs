using TuringSimulator.Core.Types;
using UnityEngine;
using UnityEngine.Serialization;

namespace TuringSimulator.Core.Validation
{
    [CreateAssetMenu(
        menuName = "Turing Simulator/Level Test",
        fileName = "Level Test")]
    public class ValidationTest : ScriptableObject
    {
        [SerializeField] public int headIndex;
        [SerializeField] public Symbol[] initialSymbols;
        [SerializeField] public int expectedHeadIndex;
        [SerializeField] public HaltStatus expectedStatus;
        [SerializeField] public Symbol[] expectedSymbols;
    }
}