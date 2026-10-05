using System;
using NUnit.Framework;
using TuringSimulator.Core.ProgramGraph;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;
using TuringSimulator.GameFlow;
using ITS;

namespace EditModeTests
{
    public class ItsBenchStateCacheTests
    {
        [Test]
        public void GetTapeForAsk_ReusesCacheUntilDirty()
        {
            var tape = new SimulationTape(0, Symbol.Gear);
            var captures = 0;
            var cache = new ItsBenchStateCache(
                () =>
                {
                    captures++;
                    return tape.Snapshot();
                },
                () => null);

            var first = cache.GetTapeForAsk();
            var second = cache.GetTapeForAsk();

            Assert.That(captures, Is.EqualTo(1));
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.Cells, Does.Contain(ItsBenchTapeCompact.Engrenagem));

            tape.Write(Symbol.Nut);
            cache.MarkTapeDirty();
            var third = cache.GetTapeForAsk();

            Assert.That(captures, Is.EqualTo(2));
            Assert.That(third, Is.Not.SameAs(first));
            Assert.That(third.Cells, Does.Contain(ItsBenchTapeCompact.Porca));
        }

        [Test]
        public void GetProgramForAsk_ReusesCacheUntilDirty()
        {
            var captures = 0;
            var snap = new ProgramGraphSnapshot(
                new[] { new ProgramGraphNodeData("m1", ProgramBlockKind.Move, null, MoveDirection.Right) },
                Array.Empty<ProgramGraphEdgeData>(),
                "m1");
            var cache = new ItsBenchStateCache(
                () => null,
                () =>
                {
                    captures++;
                    return snap;
                });

            var first = cache.GetProgramForAsk();
            var second = cache.GetProgramForAsk();
            Assert.That(captures, Is.EqualTo(1));
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.TomadaLigada, Is.True);
            Assert.That(first.Blocos[0].Tipo, Is.EqualTo(ItsBenchProgramSerializer.TipoMovimento));

            cache.MarkProgramDirty();
            var third = cache.GetProgramForAsk();
            Assert.That(captures, Is.EqualTo(2));
            Assert.That(third, Is.Not.SameAs(first));
        }

        [Test]
        public void DirtyFlags_AreIndependent()
        {
            var tapeCaptures = 0;
            var programCaptures = 0;
            var cache = new ItsBenchStateCache(
                () =>
                {
                    tapeCaptures++;
                    return new SimulationTape(0).Snapshot();
                },
                () =>
                {
                    programCaptures++;
                    return new ProgramGraphSnapshot(
                        Array.Empty<ProgramGraphNodeData>(),
                        Array.Empty<ProgramGraphEdgeData>(),
                        string.Empty);
                });

            cache.GetTapeForAsk();
            cache.GetProgramForAsk();
            cache.MarkTapeDirty();
            cache.GetTapeForAsk();
            cache.GetProgramForAsk();

            Assert.That(tapeCaptures, Is.EqualTo(2));
            Assert.That(programCaptures, Is.EqualTo(1));
        }
    }
}
