using System.Collections.Generic;
using NUnit.Framework;
using TuringSimulator.Core.Types;
using TuringSimulator.View.Machine.Tape;
using UnityEngine;

namespace EditModeTests
{
    public class TapeCellSymbolBindingTests
    {
        private sealed class FakeCatalog : ITapeSymbolPrefabs
        {
            private readonly Dictionary<Symbol, GameObject> _prefabs;

            public FakeCatalog(Dictionary<Symbol, GameObject> prefabs)
            {
                _prefabs = prefabs;
            }

            public bool TryGetPrefab(Symbol symbol, out GameObject prefab)
            {
                return _prefabs.TryGetValue(symbol, out prefab) && prefab != null;
            }
        }

        [Test]
        public void ShouldClear_BlankAndNone()
        {
            Assert.That(TapeCellSymbolBinding.ShouldClear(Symbol.Blank), Is.True);
            Assert.That(TapeCellSymbolBinding.ShouldClear(Symbol.None), Is.True);
        }

        [Test]
        public void ShouldClear_PhysicalSymbols_IsFalse()
        {
            Assert.That(TapeCellSymbolBinding.ShouldClear(Symbol.Gear), Is.False);
            Assert.That(TapeCellSymbolBinding.ShouldClear(Symbol.Screw), Is.False);
            Assert.That(TapeCellSymbolBinding.ShouldClear(Symbol.Nut), Is.False);
        }

        [Test]
        public void ResolvePrefab_Blank_ReturnsNull()
        {
            var gear = new GameObject("gear");
            var catalog = new FakeCatalog(new Dictionary<Symbol, GameObject>
            {
                { Symbol.Gear, gear }
            });

            Assert.That(TapeCellSymbolBinding.ResolvePrefab(Symbol.Blank, catalog), Is.Null);
            Object.DestroyImmediate(gear);
        }

        [Test]
        public void ResolvePrefab_Gear_ReturnsCatalogPrefab()
        {
            var gear = new GameObject("gear");
            var catalog = new FakeCatalog(new Dictionary<Symbol, GameObject>
            {
                { Symbol.Gear, gear }
            });

            Assert.That(TapeCellSymbolBinding.ResolvePrefab(Symbol.Gear, catalog), Is.SameAs(gear));
            Object.DestroyImmediate(gear);
        }

        [Test]
        public void ResolvePrefab_MissingSymbol_ReturnsNull()
        {
            var catalog = new FakeCatalog(new Dictionary<Symbol, GameObject>());
            Assert.That(TapeCellSymbolBinding.ResolvePrefab(Symbol.Mark, catalog), Is.Null);
        }

        [Test]
        public void ResolveInstanceParent_UsesCellWhenRootIsNotAChild()
        {
            var root = new GameObject("Cell Root");
            var cell = new GameObject("TapeCell");
            var spawn = new GameObject("Spawn");
            cell.transform.SetParent(root.transform);
            spawn.transform.SetParent(cell.transform);

            Assert.That(TapeCellSymbolBinding.ResolveInstanceParent(cell.transform, null), Is.EqualTo(cell.transform));
            Assert.That(TapeCellSymbolBinding.ResolveInstanceParent(cell.transform, cell.transform), Is.EqualTo(cell.transform));
            Assert.That(TapeCellSymbolBinding.ResolveInstanceParent(cell.transform, spawn.transform), Is.EqualTo(spawn.transform));
            Assert.That(TapeCellSymbolBinding.ResolveInstanceParent(cell.transform, root.transform), Is.EqualTo(cell.transform));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TapeSymbolPrefabs_MapsGearBoltNut_NotBlank()
        {
            var gear = new GameObject("gear");
            var bolt = new GameObject("bolt");
            var nut = new GameObject("nut");
            var catalog = ScriptableObject.CreateInstance<TapeSymbolPrefabs>();

            var gearField = typeof(TapeSymbolPrefabs).GetField(
                "gearPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var boltField = typeof(TapeSymbolPrefabs).GetField(
                "boltPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nutField = typeof(TapeSymbolPrefabs).GetField(
                "nutPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            gearField.SetValue(catalog, gear);
            boltField.SetValue(catalog, bolt);
            nutField.SetValue(catalog, nut);

            Assert.That(catalog.TryGetPrefab(Symbol.Gear, out var gearPrefab), Is.True);
            Assert.That(gearPrefab, Is.SameAs(gear));
            Assert.That(catalog.TryGetPrefab(Symbol.Screw, out var boltPrefab), Is.True);
            Assert.That(boltPrefab, Is.SameAs(bolt));
            Assert.That(catalog.TryGetPrefab(Symbol.Nut, out var nutPrefab), Is.True);
            Assert.That(nutPrefab, Is.SameAs(nut));
            Assert.That(catalog.TryGetPrefab(Symbol.Blank, out _), Is.False);

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(gear);
            Object.DestroyImmediate(bolt);
            Object.DestroyImmediate(nut);
        }
    }
}
