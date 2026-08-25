using System.Collections.Generic;
using NUnit.Framework;
using TuringSimulator.Core.Types;
using TuringSimulator.View.Machine.Tape;
using UnityEngine;

namespace EditModeTests
{
    public class ConveyorTapeWindowTests
    {
        [Test]
        public void ResolveCellSymbol_UsesSparseTapeNotCount()
        {
            var tape = new Dictionary<int, Symbol>
            {
                { -1, Symbol.Nut },
                { 0, Symbol.Gear }
            };

            const int firstTapeIndex = -5;
            Assert.That(
                ConveyorTapeWindow.ResolveCellSymbol(tape, firstTapeIndex, cellIndex: 5),
                Is.EqualTo(Symbol.Gear));
            Assert.That(
                ConveyorTapeWindow.ResolveCellSymbol(tape, firstTapeIndex, cellIndex: 4),
                Is.EqualTo(Symbol.Nut));
            Assert.That(
                ConveyorTapeWindow.ResolveCellSymbol(tape, firstTapeIndex, cellIndex: 6),
                Is.EqualTo(Symbol.Blank));
        }

        [Test]
        public void FirstTapeIndex_CentersTheInitialPoolOnTheHead()
        {
            Assert.That(ConveyorTapeWindow.FirstTapeIndex(originHeadIndex: 0, cellCount: 11), Is.EqualTo(-5));
            Assert.That(ConveyorTapeWindow.LastTapeIndex(-5, 11), Is.EqualTo(5));
        }

        [Test]
        public void CellIndexForTapeIndex_UsesFirstTapeIndex()
        {
            Assert.That(ConveyorTapeWindow.CellIndexForTapeIndex(-5, 0), Is.EqualTo(5));
            Assert.That(ConveyorTapeWindow.CellIndexForTapeIndex(-5, 1), Is.EqualTo(6));
            Assert.That(ConveyorTapeWindow.CellIndexForTapeIndex(-5, -1), Is.EqualTo(4));
        }

        [Test]
        public void TryGetCellIndex_RejectsCellsOutsideThePool()
        {
            Assert.That(
                ConveyorTapeWindow.TryGetCellIndex(-5, tapeIndex: 5, cellCount: 11, out var inside),
                Is.True);
            Assert.That(inside, Is.EqualTo(10));
            Assert.That(
                ConveyorTapeWindow.TryGetCellIndex(-5, tapeIndex: 6, cellCount: 11, out _),
                Is.False);
        }

        [Test]
        public void TryGetGrowDirection_WhenHeadLeavesThePool()
        {
            Assert.That(
                ConveyorTapeWindow.TryGetGrowDirection(-5, 11, tapeIndex: 6, out var right),
                Is.True);
            Assert.That(right, Is.EqualTo(MoveDirection.Right));
            Assert.That(
                ConveyorTapeWindow.TryGetGrowDirection(-5, 11, tapeIndex: -6, out var left),
                Is.True);
            Assert.That(left, Is.EqualTo(MoveDirection.Left));
            Assert.That(
                ConveyorTapeWindow.TryGetGrowDirection(-5, 11, tapeIndex: 0, out var stay),
                Is.False);
            Assert.That(stay, Is.EqualTo(MoveDirection.Stay));
        }

        [Test]
        public void MoveRootDeltaX_SlidesCellsOppositeTheHead()
        {
            Assert.That(ConveyorTapeWindow.MoveRootDeltaX(MoveDirection.Right, 1.5f), Is.EqualTo(-1.5f));
            Assert.That(ConveyorTapeWindow.MoveRootDeltaX(MoveDirection.Left, 1.5f), Is.EqualTo(1.5f));
            Assert.That(ConveyorTapeWindow.MoveRootDeltaX(MoveDirection.Stay, 1.5f), Is.EqualTo(0f));
        }

        [Test]
        public void CellLocalPosition_CentersTheHeadCell()
        {
            Assert.That(
                ConveyorTapeWindow.CellLocalPosition(5, 11, 1f),
                Is.EqualTo(Vector3.zero));
            Assert.That(
                ConveyorTapeWindow.CellLocalPosition(6, 11, 1f),
                Is.EqualTo(Vector3.right));
            Assert.That(
                ConveyorTapeWindow.CellLocalPosition(4, 11, 1f),
                Is.EqualTo(Vector3.left));
        }

        [Test]
        public void CellLocalPositionForTapeIndex_StaysStableWhenThePoolGrows()
        {
            Assert.That(
                ConveyorTapeWindow.CellLocalPositionForTapeIndex(0, originHeadIndex: 0, 1f),
                Is.EqualTo(Vector3.zero));
            Assert.That(
                ConveyorTapeWindow.CellLocalPositionForTapeIndex(6, originHeadIndex: 0, 1f),
                Is.EqualTo(Vector3.right * 6f));
        }
    }
}
