using System;
using System.Collections.Generic;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;

namespace Tests.EditModeTests.Mocks
{
    public class TestSimulationTape : ITape
    {
        private readonly Dictionary<int, Symbol> _cells = new();

        public int HeadIndex { get; private set; }

        public Symbol CurrentSymbol => Read();
        public IReadOnlyDictionary<int, Symbol> Cells => _cells;

        public TestSimulationTape(params Symbol[] initialSymbols)
        {
            HeadIndex = 0;

            for (int i = 0; i < initialSymbols.Length; i++)
            {
                _cells[i] = initialSymbols[i];
            }
        }

        public Symbol Read()
        {
            return _cells.GetValueOrDefault(HeadIndex, Symbol.Blank);
        }

        public void Write(Symbol symbol)
        {
            _cells[HeadIndex] = symbol;
        }

        public void Move(MoveDirection direction)
        {
            switch (direction)
            {
                case MoveDirection.Left:
                    HeadIndex--;
                    break;

                case MoveDirection.Right:
                    HeadIndex++;
                    break;
                
                default:
                case MoveDirection.Stay:
                    break;
            }
        }
        
        /// <summary>
        /// Returns a snapshot of the tape content in order from min to max index.
        /// Useful for tests.
        /// </summary>
        /// <returns>An array of symbols representing the tape content.</returns>
        public Symbol[] ToArray()
        {
            if (_cells.Count == 0)
                return Array.Empty<Symbol>();

            var min = int.MaxValue;
            var max = int.MinValue;

            foreach (var key in _cells.Keys)
            {
                if (key < min) min = key;
                if (key > max) max = key;
            }

            var result = new Symbol[max - min + 1];
            for (int i = min; i <= max; i++)
            {
                result[i - min] = _cells.GetValueOrDefault(i, Symbol.Blank);
            }

            return result;
        }
        
        // Optional helper for tests
        public void ResetHead(int index = 0)
        {
            HeadIndex = index;
        }
    }
}