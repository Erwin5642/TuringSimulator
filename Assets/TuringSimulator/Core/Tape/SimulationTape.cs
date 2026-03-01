using System;
using System.Collections.Generic;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Tape
{
    public sealed class SimulationTape : ITape
    {
        private readonly Dictionary<int, Symbol> _cells = new();

        public int HeadIndex { get; private set; }

        public Symbol CurrentSymbol => Read();
        
        public SimulationTape(int headIndex, params Symbol[] initialSymbols)
        {
            HeadIndex = headIndex;
            for (int i = 0; i < initialSymbols.Length; i++)
                _cells[i] = initialSymbols[i];
        }

        public Symbol Read()
            => _cells.GetValueOrDefault(HeadIndex, Symbol.Blank);

        public void Write(Symbol symbol)
            => _cells[HeadIndex] = symbol;

        public void Move(MoveDirection direction)
        {
            if (direction == MoveDirection.Left) HeadIndex--;
            else if (direction == MoveDirection.Right) HeadIndex++;
        }

        public void Clear()
        {
            HeadIndex = 0;
            _cells.Clear();
        }

        public TapeSnapshot Snapshot()
            => new TapeSnapshot(_cells, HeadIndex);
    }
}