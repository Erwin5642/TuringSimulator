using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Tape
{
    public interface ITapeSymbolPrefabs
    {
        bool TryGetPrefab(Symbol symbol, out GameObject prefab);
    }
}
