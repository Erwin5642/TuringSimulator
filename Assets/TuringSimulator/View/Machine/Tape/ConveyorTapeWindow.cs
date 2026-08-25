using System.Collections.Generic;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Tape
{
    public static class ConveyorTapeWindow
    {
        public static int CenterIndex(int cellCount)
        {
            return cellCount / 2;
        }

        public static int FirstTapeIndex(int originHeadIndex, int cellCount)
        {
            return originHeadIndex - CenterIndex(cellCount);
        }

        public static int LastTapeIndex(int firstTapeIndex, int cellCount)
        {
            return firstTapeIndex + cellCount - 1;
        }

        public static int TapeIndexForCell(int firstTapeIndex, int cellIndex)
        {
            return firstTapeIndex + cellIndex;
        }

        public static int CellIndexForTapeIndex(int firstTapeIndex, int tapeIndex)
        {
            return tapeIndex - firstTapeIndex;
        }

        public static bool TryGetCellIndex(
            int firstTapeIndex,
            int tapeIndex,
            int cellCount,
            out int cellIndex)
        {
            cellIndex = CellIndexForTapeIndex(firstTapeIndex, tapeIndex);
            return cellCount > 0 && cellIndex >= 0 && cellIndex < cellCount;
        }

        public static bool TryGetGrowDirection(
            int firstTapeIndex,
            int cellCount,
            int tapeIndex,
            out MoveDirection direction)
        {
            if (cellCount <= 0)
            {
                direction = MoveDirection.Stay;
                return false;
            }

            if (tapeIndex < firstTapeIndex)
            {
                direction = MoveDirection.Left;
                return true;
            }

            if (tapeIndex > LastTapeIndex(firstTapeIndex, cellCount))
            {
                direction = MoveDirection.Right;
                return true;
            }

            direction = MoveDirection.Stay;
            return false;
        }

        public static Symbol ResolveCellSymbol(
            IReadOnlyDictionary<int, Symbol> tape,
            int firstTapeIndex,
            int cellIndex)
        {
            if (tape == null)
                throw new System.ArgumentNullException(nameof(tape));

            int tapeIndex = TapeIndexForCell(firstTapeIndex, cellIndex);
            return tape.TryGetValue(tapeIndex, out var symbol) ? symbol : Symbol.Blank;
        }

        public static Vector3 CellLocalPosition(int cellIndex, int cellCount, float cellSpacing)
        {
            return Vector3.right * ((cellIndex - CenterIndex(cellCount)) * cellSpacing);
        }

        public static Vector3 CellLocalPositionForTapeIndex(
            int tapeIndex,
            int originHeadIndex,
            float cellSpacing)
        {
            return Vector3.right * ((tapeIndex - originHeadIndex) * cellSpacing);
        }

        public static float MoveRootDeltaX(MoveDirection direction, float cellSpacing)
        {
            return direction switch
            {
                MoveDirection.Right => -cellSpacing,
                MoveDirection.Left => cellSpacing,
                _ => 0f
            };
        }
    }
}
