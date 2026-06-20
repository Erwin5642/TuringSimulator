using System.Collections.Generic;
using NUnit.Framework;
using TuringSimulator.Controller;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.ProgramGraph;
using TuringSimulator.Core.Types;

namespace EditModeTests
{
    public class GraphToProgramCompilerTests
    {
        [Test]
        public void Linear_WriteMoveAccept_Compiles()
        {
            var nodes = new[]
            {
                new ProgramGraphNodeData("w", ProgramBlockKind.Write, Symbol.Gear),
                new ProgramGraphNodeData("m", ProgramBlockKind.Move, null, MoveDirection.Right),
                new ProgramGraphNodeData("a", ProgramBlockKind.Accept),
            };
            var edges = new[]
            {
                new ProgramGraphEdgeData("w", 0, "m"),
                new ProgramGraphEdgeData("m", 0, "a"),
            };
            var snap = new ProgramGraphSnapshot(nodes, edges, "w");

            Assert.That(GraphToProgramCompiler.TryCompile(snap, out var builder, out var err), Is.True, err);
            var program = builder.Build();

            Assert.That(program.StartState, Is.EqualTo(0));
            Assert.That(program.IsFinalState(2), Is.True);

            // BFS order: w=0, m=1, a=2 — Write emits full row to state 1 with symbol Gear.
            Assert.That(program.TryGetTransition(0, Symbol.Blank, out var tw), Is.True);
            Assert.That(tw.ToState, Is.EqualTo(1));
            Assert.That(tw.SymbolToWrite, Is.EqualTo(Symbol.Gear));
            Assert.That(tw.DirectionToMove, Is.EqualTo(MoveDirection.Stay));

            Assert.That(program.TryGetTransition(1, Symbol.Screw, out var tm), Is.True);
            Assert.That(tm.ToState, Is.EqualTo(2));
            Assert.That(tm.SymbolToWrite, Is.EqualTo(Symbol.Screw));
            Assert.That(tm.DirectionToMove, Is.EqualTo(MoveDirection.Right));
        }

        [Test]
        public void Cycle_WriteMoveLoop_Compiles()
        {
            var nodes = new[]
            {
                new ProgramGraphNodeData("w", ProgramBlockKind.Write, Symbol.Screw),
                new ProgramGraphNodeData("m", ProgramBlockKind.Move, null, MoveDirection.Left),
            };
            var edges = new[]
            {
                new ProgramGraphEdgeData("w", 0, "m"),
                new ProgramGraphEdgeData("m", 0, "w"),
            };
            var snap = new ProgramGraphSnapshot(nodes, edges, "w");

            Assert.That(GraphToProgramCompiler.TryCompile(snap, out var builder, out var err), Is.True, err);
            var program = builder.Build();

            Assert.That(program.TryGetTransition(0, Symbol.Blank, out var t0), Is.True);
            Assert.That(t0.ToState, Is.EqualTo(1));
            Assert.That(t0.SymbolToWrite, Is.EqualTo(Symbol.Screw));

            Assert.That(program.TryGetTransition(1, Symbol.Mark, out var t1), Is.True);
            Assert.That(t1.ToState, Is.EqualTo(0));
            Assert.That(t1.SymbolToWrite, Is.EqualTo(Symbol.Mark));
            Assert.That(t1.DirectionToMove, Is.EqualTo(MoveDirection.Left));
        }

        [Test]
        public void Disconnected_Block_Fails()
        {
            var nodes = new[]
            {
                new ProgramGraphNodeData("w", ProgramBlockKind.Write, Symbol.Gear),
                new ProgramGraphNodeData("u", ProgramBlockKind.Accept),
            };
            var edges = new List<ProgramGraphEdgeData>();
            var snap = new ProgramGraphSnapshot(nodes, edges, "w");

            Assert.That(GraphToProgramCompiler.TryCompile(snap, out _, out var err), Is.False);
            Assert.That(err, Does.Contain("unreachable"));
        }

        [Test]
        public void Accept_WithOutgoingWire_Fails()
        {
            var nodes = new[]
            {
                new ProgramGraphNodeData("a", ProgramBlockKind.Accept),
                new ProgramGraphNodeData("w", ProgramBlockKind.Write, Symbol.Gear),
            };
            var edges = new[]
            {
                new ProgramGraphEdgeData("a", 0, "w"),
            };
            var snap = new ProgramGraphSnapshot(nodes, edges, "a");

            Assert.That(GraphToProgramCompiler.TryCompile(snap, out _, out var err), Is.False);
            Assert.That(err, Does.Contain("Terminal block"));
        }
    }
}
