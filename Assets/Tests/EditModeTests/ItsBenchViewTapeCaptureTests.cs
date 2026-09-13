using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;
using TuringSimulator.GameFlow;
using TuringSimulator.View.Machine.Tape;
using ITS;

namespace EditModeTests
{
    public class ItsBenchViewTapeCaptureTests
    {
        [Test]
        public void GetTapeForAsk_UsesViewSnapshot_NotMismatchedModelTape()
        {
            var view = new RecordingTapeVisual();
            view.SetTape(new[] { Symbol.Nut }, headIndex: 0);
            var modelTape = new SimulationTape(0, Symbol.Gear);
            var cache = new ItsBenchStateCache(
                () => view.Snapshot(),
                () => null);

            var dto = cache.GetTapeForAsk();

            Assert.That(dto.Cells, Does.Contain(ItsBenchTapeCompact.Porca));
            Assert.That(dto.Cells, Does.Not.Contain(ItsBenchTapeCompact.Engrenagem));
            Assert.That(modelTape.CurrentSymbol, Is.EqualTo(Symbol.Gear));
        }

        [Test]
        public void GetTapeForAsk_EmptyView_IsVazio_NotHiddenLot()
        {
            var view = new RecordingTapeVisual();
            view.SetTape(Array.Empty<Symbol>(), headIndex: 0);
            var cache = new ItsBenchStateCache(
                () => view.Snapshot(),
                () => null);

            var dto = cache.GetTapeForAsk();

            Assert.That(dto.Cells, Is.EqualTo(new[] { ItsBenchTapeCompact.Vazio }));
            Assert.That(dto.HeadOffset, Is.EqualTo(0));
            Assert.That(dto.Cells, Does.Not.Contain(ItsBenchTapeCompact.Engrenagem));
        }

        sealed class RecordingTapeVisual : ITapeVisual
        {
            readonly Dictionary<int, Symbol> _cells = new();

            public int HeadIndex { get; private set; }

            public void Initialize()
            {
            }

            public void SetTape(IReadOnlyList<Symbol> symbols, int headIndex)
            {
                _cells.Clear();
                if (symbols != null)
                {
                    for (var i = 0; i < symbols.Count; i++)
                        _cells[i] = symbols[i];
                }

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
                _cells.Clear();
                HeadIndex = 0;
            }

            public TapeSnapshot Snapshot()
                => new TapeSnapshot(_cells, HeadIndex);
        }
    }
}
