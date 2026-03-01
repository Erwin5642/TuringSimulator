using System;
using System.Collections.Generic;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Tape
{
    public interface ITape
    {
        int HeadIndex { get; }
        Symbol CurrentSymbol { get; }

        Symbol Read();
        void Write(Symbol symbol);
        void Move(MoveDirection direction);
    }
}