using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TuringSimulator.Core.Types;
using TuringSimulator.GameFlow.Events;
using TuringSimulator.View.Machine.Tape;
using UnityEngine;

namespace EditModeTests
{
    public class ConveyorTapeVisualTests
    {
        [Test]
        public void Initialize_UsesExistingPool_DoesNotClone()
        {
            var setup = CreateVisual(templateCells: 3);

            setup.Visual.Initialize();

            Assert.That(setup.CellsRoot.GetComponentsInChildren<TapeCellView>(true).Length, Is.EqualTo(3));

            setup.Destroy();
        }

        [Test]
        public void SetTape_LeavesBlankCellsInactive()
        {
            var setup = CreateVisual(templateCells: 5);
            setup.Visual.Initialize();
            setup.Visual.SetTape(new[] { Symbol.Gear }, headIndex: 0);

            var cells = SortedCells(setup.CellsRoot);
            Assert.That(cells[2].gameObject.activeSelf, Is.True);
            Assert.That(cells[2].transform.childCount, Is.EqualTo(1));
            Assert.That(cells[0].gameObject.activeSelf, Is.False);
            Assert.That(cells[1].gameObject.activeSelf, Is.False);
            Assert.That(cells[3].gameObject.activeSelf, Is.False);
            Assert.That(cells[4].gameObject.activeSelf, Is.False);

            setup.Destroy();
        }

        [Test]
        public void MoveHead_KeepsCellRootOffset_AndLeavesCellLocals()
        {
            var setup = CreateVisual(templateCells: 5);
            setup.Visual.Initialize();
            setup.Visual.SetTape(new[] { Symbol.Gear }, headIndex: 0);

            var cellLocals = CaptureCellLocals(setup.CellsRoot);
            Drain(setup.Visual.MoveHead(MoveDirection.Right));

            Assert.That(setup.CellsRoot.localPosition.x, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(setup.Visual.HeadIndex, Is.EqualTo(1));
            AssertSameLocals(setup.CellsRoot, cellLocals);

            setup.Destroy();
        }

        [Test]
        public void MoveHead_GrowsACellWhenHeadLeavesThePool()
        {
            var setup = CreateVisual(templateCells: 3);
            setup.Visual.Initialize();
            setup.Visual.SetTape(new[] { Symbol.Blank }, headIndex: 0);

            Drain(setup.Visual.MoveHead(MoveDirection.Right));
            Assert.That(SortedCells(setup.CellsRoot).Length, Is.EqualTo(3));

            Drain(setup.Visual.MoveHead(MoveDirection.Right));
            var cells = SortedCells(setup.CellsRoot);
            Assert.That(cells.Length, Is.EqualTo(4));
            Assert.That(setup.Visual.HeadIndex, Is.EqualTo(2));
            Assert.That(cells[3].gameObject.activeSelf, Is.False);

            setup.Destroy();
        }

        [Test]
        public void MoveHead_RaisesTapeMovedStartedAndFinished()
        {
            var setup = CreateVisual(templateCells: 5);
            var channel = ScriptableObject.CreateInstance<TapeMovedEventChannel>();
            var phases = new System.Collections.Generic.List<TapeMovePhase>();
            channel.OnRaised += data => phases.Add(data.Phase);
            SetField(setup.Visual, "tapeMovedChannel", channel);
            setup.Visual.Initialize();
            setup.Visual.SetTape(new[] { Symbol.Gear }, headIndex: 0);

            Drain(setup.Visual.MoveHead(MoveDirection.Right));
            Drain(setup.Visual.MoveHead(MoveDirection.Stay));

            Assert.That(phases, Is.EqualTo(new[]
            {
                TapeMovePhase.Started,
                TapeMovePhase.Finished,
            }));

            Object.DestroyImmediate(channel);
            setup.Destroy();
        }

        [Test]
        public void ShowRead_RaisesTapeReadStartedAndFinished()
        {
            var setup = CreateVisual(templateCells: 5);
            var channel = ScriptableObject.CreateInstance<TapeReadEventChannel>();
            var phases = new System.Collections.Generic.List<TapeReadPhase>();
            var matches = new System.Collections.Generic.List<bool>();
            channel.OnRaised += data =>
            {
                phases.Add(data.Phase);
                matches.Add(data.IsMatch);
            };
            SetField(setup.Visual, "tapeReadChannel", channel);
            setup.Visual.Initialize();
            setup.Visual.SetTape(new[] { Symbol.Gear }, headIndex: 0);

            Drain(setup.Visual.ShowRead(Symbol.Gear, Symbol.Nut));

            Assert.That(phases, Is.EqualTo(new[]
            {
                TapeReadPhase.Started,
                TapeReadPhase.Finished,
            }));
            Assert.That(matches, Is.EqualTo(new[] { false, false }));

            Object.DestroyImmediate(channel);
            setup.Destroy();
        }

        [Test]
        public void ShowWrite_RaisesTapeWriteOnWriteAndDelete_NotOnNoOp()
        {
            var setup = CreateVisual(templateCells: 5);
            var channel = ScriptableObject.CreateInstance<TapeWriteEventChannel>();
            var raised = new System.Collections.Generic.List<(TapeWritePhase Phase, TapeWriteKind Effect)>();
            channel.OnRaised += data => raised.Add((data.Phase, data.Effect));
            SetField(setup.Visual, "tapeWriteChannel", channel);
            setup.Visual.Initialize();
            setup.Visual.SetTape(new[] { Symbol.Blank }, headIndex: 0);

            Drain(setup.Visual.ShowWrite(Symbol.Gear));
            Drain(setup.Visual.ShowWrite(Symbol.Gear));
            Drain(setup.Visual.ShowWrite(Symbol.Blank));

            Assert.That(raised, Is.EqualTo(new[]
            {
                (TapeWritePhase.Started, TapeWriteKind.Write),
                (TapeWritePhase.Finished, TapeWriteKind.Write),
                (TapeWritePhase.Started, TapeWriteKind.Delete),
                (TapeWritePhase.Finished, TapeWriteKind.Delete),
            }));

            Object.DestroyImmediate(channel);
            setup.Destroy();
        }

        [Test]
        public void ShowWrite_ActivatesTheHeadCell_AndBlankDeactivatesIt()
        {
            var setup = CreateVisual(templateCells: 5);
            setup.Visual.Initialize();
            setup.Visual.SetTape(new[] { Symbol.Blank }, headIndex: 0);

            Drain(setup.Visual.ShowWrite(Symbol.Gear));

            var cells = SortedCells(setup.CellsRoot);
            Assert.That(cells[2].gameObject.activeSelf, Is.True);
            Assert.That(cells[2].transform.childCount, Is.EqualTo(1));
            Assert.That(cells[2].transform.GetChild(0).parent, Is.EqualTo(cells[2].transform));
            Assert.That(setup.CellsRoot.childCount, Is.EqualTo(5));

            Drain(setup.Visual.ShowWrite(Symbol.Blank));
            Assert.That(cells[2].gameObject.activeSelf, Is.False);
            Assert.That(cells[2].transform.childCount, Is.EqualTo(0));

            setup.Destroy();
        }

        [Test]
        public void ShowRead_ForwardsSymbolsToFeedback()
        {
            var setup = CreateVisual(templateCells: 5);
            var feedback = setup.Tape.AddComponent<RecordingTapeStepFeedback>();
            SetField(setup.Visual, "stepFeedback", feedback);
            setup.Visual.Initialize();
            setup.Visual.SetTape(new[] { Symbol.Gear }, headIndex: 0);

            Drain(setup.Visual.ShowRead(Symbol.Gear, Symbol.Gear));

            Assert.That(feedback.Reads.Count, Is.EqualTo(1));
            Assert.That(feedback.Reads[0].Read, Is.EqualTo(Symbol.Gear));
            Assert.That(feedback.Reads[0].Write, Is.EqualTo(Symbol.Gear));
            Assert.That(feedback.Writes, Is.Empty);

            setup.Destroy();
        }

        [Test]
        public void ShowWrite_RequestsWriteAndDeleteEffects()
        {
            var setup = CreateVisual(templateCells: 5);
            var feedback = setup.Tape.AddComponent<RecordingTapeStepFeedback>();
            SetField(setup.Visual, "stepFeedback", feedback);
            setup.Visual.Initialize();
            setup.Visual.SetTape(new[] { Symbol.Blank }, headIndex: 0);

            Drain(setup.Visual.ShowWrite(Symbol.Gear));
            Drain(setup.Visual.ShowWrite(Symbol.Gear));
            Drain(setup.Visual.ShowWrite(Symbol.Blank));

            Assert.That(feedback.Writes, Is.EqualTo(new[]
            {
                TapeWriteEffectKind.Write,
                TapeWriteEffectKind.Delete,
            }));

            setup.Destroy();
        }

        private static Setup CreateVisual(int templateCells)
        {
            var gearPrefab = new GameObject("gearPrefab");
            var catalog = ScriptableObject.CreateInstance<TapeSymbolPrefabs>();
            typeof(TapeSymbolPrefabs)
                .GetField("gearPrefab", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(catalog, gearPrefab);

            var tape = new GameObject("Tape");
            var visual = tape.AddComponent<ConveyorTapeVisual>();
            var cellsRoot = new GameObject("Cell Root");
            cellsRoot.transform.SetParent(tape.transform);

            for (int i = 0; i < templateCells; i++)
            {
                var cell = new GameObject($"TapeCell ({i})");
                cell.transform.SetParent(cellsRoot.transform);
                cell.transform.localPosition = Vector3.right * i;
                var view = cell.AddComponent<TapeCellView>();
                typeof(TapeCellView)
                    .GetField("symbolPrefabs", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(view, catalog);
            }

            SetField(visual, "cellsRoot", cellsRoot.transform);
            SetField(visual, "cellSpacing", 1f);
            SetField(visual, "moveDuration", 0f);

            return new Setup(tape, visual, cellsRoot.transform, catalog, gearPrefab);
        }

        private static void SetField(object target, string name, object value)
        {
            typeof(ConveyorTapeVisual)
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(target, value);
        }

        private static void Drain(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
            }
        }

        private static TapeCellView[] SortedCells(Transform cellsRoot)
        {
            var cells = cellsRoot.GetComponentsInChildren<TapeCellView>(true);
            System.Array.Sort(cells, (a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
            return cells;
        }

        private static Vector3[] CaptureCellLocals(Transform cellsRoot)
        {
            var cells = SortedCells(cellsRoot);
            var locals = new Vector3[cells.Length];
            for (int i = 0; i < cells.Length; i++)
                locals[i] = cells[i].transform.localPosition;
            return locals;
        }

        private static void AssertSameLocals(Transform cellsRoot, Vector3[] expected)
        {
            var cells = SortedCells(cellsRoot);
            Assert.That(cells.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < cells.Length; i++)
                Assert.That(cells[i].transform.localPosition, Is.EqualTo(expected[i]));
        }

        private sealed class Setup
        {
            public Setup(
                GameObject tape,
                ConveyorTapeVisual visual,
                Transform cellsRoot,
                TapeSymbolPrefabs catalog,
                GameObject gearPrefab)
            {
                Tape = tape;
                Visual = visual;
                CellsRoot = cellsRoot;
                Catalog = catalog;
                GearPrefab = gearPrefab;
            }

            public GameObject Tape { get; }
            public ConveyorTapeVisual Visual { get; }
            public Transform CellsRoot { get; }
            public TapeSymbolPrefabs Catalog { get; }
            public GameObject GearPrefab { get; }

            public void Destroy()
            {
                Object.DestroyImmediate(Tape);
                Object.DestroyImmediate(Catalog);
                Object.DestroyImmediate(GearPrefab);
            }
        }

        private sealed class RecordingTapeStepFeedback : MonoBehaviour, ITapeStepFeedback
        {
            public readonly System.Collections.Generic.List<(Symbol Read, Symbol Write)> Reads = new();
            public readonly System.Collections.Generic.List<TapeWriteEffectKind> Writes = new();

            public IEnumerator PlayRead(Symbol readSymbol, Symbol writeSymbol, Vector3 worldPosition)
            {
                _ = worldPosition;
                Reads.Add((readSymbol, writeSymbol));
                yield break;
            }

            public IEnumerator PlayWrite(TapeWriteEffectKind kind, Vector3 worldPosition)
            {
                _ = worldPosition;
                Writes.Add(kind);
                yield break;
            }
        }
    }
}
