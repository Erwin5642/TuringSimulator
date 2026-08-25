using System;
using NUnit.Framework;
using TuringSimulator.Core.ProgramGraph;
using TuringSimulator.Core.Types;

namespace EditModeTests
{
    public class ProgramGraphFingerprintTests
    {
        [Test]
        public void Compute_IsOrderIndependent_ForNodesAndEdges()
        {
            var snapA = new ProgramGraphSnapshot(
                new[]
                {
                    new ProgramGraphNodeData("w", ProgramBlockKind.Write, Symbol.Gear),
                    new ProgramGraphNodeData("m", ProgramBlockKind.Move, null, MoveDirection.Right),
                },
                new[]
                {
                    new ProgramGraphEdgeData("w", 0, "m"),
                },
                "w");

            var snapB = new ProgramGraphSnapshot(
                new[]
                {
                    new ProgramGraphNodeData("m", ProgramBlockKind.Move, null, MoveDirection.Right),
                    new ProgramGraphNodeData("w", ProgramBlockKind.Write, Symbol.Gear),
                },
                new[]
                {
                    new ProgramGraphEdgeData("w", 0, "m"),
                },
                "w");

            Assert.That(ProgramGraphFingerprint.Compute(snapA), Is.EqualTo(ProgramGraphFingerprint.Compute(snapB)));
        }

        [Test]
        public void Compute_Changes_WhenCardChanges()
        {
            var withGear = new ProgramGraphSnapshot(
                new[] { new ProgramGraphNodeData("w", ProgramBlockKind.Write, Symbol.Gear) },
                Array.Empty<ProgramGraphEdgeData>(),
                "w");
            var withScrew = new ProgramGraphSnapshot(
                new[] { new ProgramGraphNodeData("w", ProgramBlockKind.Write, Symbol.Screw) },
                Array.Empty<ProgramGraphEdgeData>(),
                "w");

            Assert.That(
                ProgramGraphFingerprint.Compute(withGear),
                Is.Not.EqualTo(ProgramGraphFingerprint.Compute(withScrew)));
        }

        [Test]
        public void Compute_Changes_WhenEdgeDirectionChanges()
        {
            var aToB = new ProgramGraphSnapshot(
                new[]
                {
                    new ProgramGraphNodeData("a", ProgramBlockKind.Move, null, MoveDirection.Right),
                    new ProgramGraphNodeData("b", ProgramBlockKind.Move, null, MoveDirection.Left),
                },
                new[] { new ProgramGraphEdgeData("a", 0, "b") },
                "a");
            var bToA = new ProgramGraphSnapshot(
                new[]
                {
                    new ProgramGraphNodeData("a", ProgramBlockKind.Move, null, MoveDirection.Right),
                    new ProgramGraphNodeData("b", ProgramBlockKind.Move, null, MoveDirection.Left),
                },
                new[] { new ProgramGraphEdgeData("b", 0, "a") },
                "a");

            Assert.That(
                ProgramGraphFingerprint.Compute(aToB),
                Is.Not.EqualTo(ProgramGraphFingerprint.Compute(bToA)));
        }
    }
}
