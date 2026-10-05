using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TuringSimulator.Core.Level;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;
using TuringSimulator.Core.Validation;
using TuringSimulator.GameFlow;
using TuringSimulator.GameFlow.Events;
using TuringSimulator.View.Machine.Tape;
using ITS;
using UnityEngine;

namespace EditModeTests
{
    public class PlayTapeMaterializeTests
    {
        readonly List<ScriptableObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        [Test]
        public void Apply_CopiesActivePlayTestOntoModelAndView_AndMarksTapeDirty()
        {
            var test = ScriptableObject.CreateInstance<ValidationTest>();
            test.scenarioId = "play";
            test.headIndex = 2;
            test.initialSymbols = new[] { Symbol.Gear, Symbol.Nut, Symbol.Screw };
            _created.Add(test);

            var database = ScriptableObject.CreateInstance<LevelDatabase>();
            _created.Add(database);
            var model = new ModelInstaller(database);
            model.ActivePlayTest = test;
            model.CurrentTape = new TuringSimulator.Core.Tape.SimulationTape(0);

            var tape = new FakeTapeVisual();
            var cache = new FakeBenchCache();
            var handler = new PlayTapeMaterializeActionHandler(cache);

            handler.Apply(model, tape);

            Assert.That(model.CurrentTape.HeadIndex, Is.EqualTo(2));
            Assert.That(model.CurrentTape.CurrentSymbol, Is.EqualTo(Symbol.Screw));
            Assert.That(tape.LastHeadIndex, Is.EqualTo(2));
            Assert.That(tape.LastSymbols, Is.EqualTo(new[] { Symbol.Gear, Symbol.Nut, Symbol.Screw }));
            Assert.That(cache.TapeDirtyCount, Is.EqualTo(1));
        }

        [Test]
        public void Apply_WithoutActivePlayTest_Throws()
        {
            var database = ScriptableObject.CreateInstance<LevelDatabase>();
            _created.Add(database);
            var model = new ModelInstaller(database);
            var handler = new PlayTapeMaterializeActionHandler(new FakeBenchCache());

            Assert.That(
                () => handler.Apply(model, new FakeTapeVisual()),
                Throws.InvalidOperationException);
        }

        sealed class FakeTapeVisual : ITapeVisual
        {
            public IReadOnlyList<Symbol> LastSymbols { get; private set; }
            public int LastHeadIndex { get; private set; }
            public int HeadIndex { get; private set; }

            public void Initialize()
            {
            }

            public void SetTape(IReadOnlyList<Symbol> symbols, int headIndex)
            {
                LastSymbols = symbols;
                LastHeadIndex = headIndex;
                HeadIndex = headIndex;
            }

            public IEnumerator MoveHead(MoveDirection direction)
            {
                yield break;
            }

            public IEnumerator ShowWrite(Symbol symbol)
            {
                yield break;
            }

            public IEnumerator ShowRead(Symbol readSymbol, Symbol writeSymbol)
            {
                yield break;
            }

            public void Reset()
            {
            }

            public TapeSnapshot Snapshot()
            {
                var cells = new Dictionary<int, Symbol>();
                if (LastSymbols != null)
                {
                    for (var i = 0; i < LastSymbols.Count; i++)
                        cells[i] = LastSymbols[i];
                }

                return new TapeSnapshot(cells, HeadIndex);
            }
        }

        sealed class FakeBenchCache : IItsBenchStateCache
        {
            public int TapeDirtyCount { get; private set; }

            public void MarkTapeDirty() => TapeDirtyCount++;

            public void MarkProgramDirty()
            {
            }

            public AskTapeDto GetTapeForAsk() => null;

            public AskProgramDto GetProgramForAsk() => null;
        }
    }
}
