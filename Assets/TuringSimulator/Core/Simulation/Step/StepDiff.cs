using System;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Simulation.Step
{
    [Serializable]
    public readonly struct StepDiff
    {
        // --- State transition ---
        public int PreviousState { get; }
        public int NextState { get; }

        // --- Tape mutation ---
        public int HeadIndexBefore { get; }
        public int HeadIndexAfter { get; }

        public Symbol SymbolBefore { get; }
        public Symbol SymbolAfter { get; }

        // --- Meta ---
        public int StepIndex { get; }
        
        public StepDiff(Symbol symbolBefore, Symbol symbolAfter,
            int headIndexBefore, int headIndexAfter,
            int previousState, int nextState,
            int stepIndex)
        {
            SymbolBefore = symbolBefore;
            SymbolAfter = symbolAfter;
            HeadIndexBefore = headIndexBefore;
            HeadIndexAfter = headIndexAfter;
            PreviousState = previousState;
            NextState = nextState;
            StepIndex = stepIndex;
        }
        
        public StepDiff Inverse()
        {
            return new StepDiff(SymbolAfter, SymbolBefore,
                HeadIndexAfter,HeadIndexBefore, 
                NextState, PreviousState, 
                StepIndex);
        }

        public MoveDirection DirectionMoved => (MoveDirection)(HeadIndexAfter - HeadIndexBefore);
    }
}