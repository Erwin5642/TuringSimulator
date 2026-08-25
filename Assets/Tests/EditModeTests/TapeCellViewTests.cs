using System.Reflection;
using NUnit.Framework;
using TuringSimulator.Core.Types;
using TuringSimulator.View.Machine.Tape;
using UnityEngine;

namespace EditModeTests
{
    public class TapeCellViewTests
    {
        [Test]
        public void SetSymbol_SpawnsPrefabChild_AndBlankDestroysIt()
        {
            var gearPrefab = new GameObject("gearPrefab");
            var catalog = ScriptableObject.CreateInstance<TapeSymbolPrefabs>();
            typeof(TapeSymbolPrefabs)
                .GetField("gearPrefab", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(catalog, gearPrefab);

            var cell = new GameObject("TapeCell");
            var view = cell.AddComponent<TapeCellView>();
            typeof(TapeCellView)
                .GetField("symbolPrefabs", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(view, catalog);

            view.SetSymbol(Symbol.Gear);
            Assert.That(cell.activeSelf, Is.True);
            Assert.That(cell.transform.childCount, Is.EqualTo(1));
            Assert.That(cell.transform.GetChild(0).name, Does.Contain("gearPrefab"));

            view.SetSymbol(Symbol.Blank);
            Assert.That(cell.activeSelf, Is.False);
            Assert.That(cell.transform.childCount, Is.EqualTo(0));

            Object.DestroyImmediate(cell);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(gearPrefab);
        }

        [Test]
        public void SetSymbol_IgnoresInstanceRootOutsideTheCell()
        {
            var gearPrefab = new GameObject("gearPrefab");
            var catalog = ScriptableObject.CreateInstance<TapeSymbolPrefabs>();
            typeof(TapeSymbolPrefabs)
                .GetField("gearPrefab", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(catalog, gearPrefab);

            var root = new GameObject("Cell Root");
            var cell = new GameObject("TapeCell");
            cell.transform.SetParent(root.transform);
            var view = cell.AddComponent<TapeCellView>();
            typeof(TapeCellView)
                .GetField("symbolPrefabs", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(view, catalog);
            typeof(TapeCellView)
                .GetField("instanceRoot", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(view, root.transform);

            view.SetSymbol(Symbol.Gear);
            Assert.That(cell.transform.childCount, Is.EqualTo(1));
            Assert.That(cell.transform.GetChild(0).parent, Is.EqualTo(cell.transform));
            Assert.That(root.transform.childCount, Is.EqualTo(1));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(gearPrefab);
        }
    }
}
