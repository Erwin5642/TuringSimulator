using System;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Tape
{
    public static class TapeCellSymbolBinding
    {
        public static bool ShouldClear(Symbol symbol)
        {
            return symbol == Symbol.Blank || symbol == Symbol.None;
        }

        public static GameObject ResolvePrefab(Symbol symbol, ITapeSymbolPrefabs catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            if (ShouldClear(symbol))
                return null;

            return catalog.TryGetPrefab(symbol, out var prefab) ? prefab : null;
        }

        public static Transform ResolveInstanceParent(Transform cell, Transform instanceRoot)
        {
            if (cell == null)
                throw new ArgumentNullException(nameof(cell));

            if (instanceRoot != null && instanceRoot.IsChildOf(cell))
                return instanceRoot;

            return cell;
        }
    }
}
