using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Tape
{
    [CreateAssetMenu(
        menuName = "Turing Simulator/Tape Symbol Prefabs",
        fileName = "TapeSymbolPrefabs")]
    public class TapeSymbolPrefabs : ScriptableObject, ITapeSymbolPrefabs
    {
        [SerializeField] private GameObject gearPrefab;
        [SerializeField] private GameObject boltPrefab;
        [SerializeField] private GameObject nutPrefab;
        [SerializeField] private GameObject markPrefab;

        public bool TryGetPrefab(Symbol symbol, out GameObject prefab)
        {
            prefab = symbol switch
            {
                Symbol.Gear => gearPrefab,
                Symbol.Screw => boltPrefab,
                Symbol.Nut => nutPrefab,
                Symbol.Mark => markPrefab,
                _ => null
            };

            return prefab != null;
        }
    }
}
