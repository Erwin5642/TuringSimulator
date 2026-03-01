using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Program
{
 
    public readonly struct Transition
    {
        public readonly int ToState; // State to transition to
        public readonly Symbol SymbolToWrite; // Symbol to write on the tape
        public readonly MoveDirection DirectionToMove; // Direction the head must move

        public Transition(int toState, Symbol symbolToWrite, MoveDirection directionToMove)
        {
            ToState = toState;
            SymbolToWrite = symbolToWrite;
            DirectionToMove = directionToMove;
        }
    }
}