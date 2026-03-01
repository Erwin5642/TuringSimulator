using System;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Simulation.Step
{
    public enum ResultKind
    {
        Diff,
        Halt
    } 
    
    
    [Serializable]
    public readonly struct StepResult
    {
        public readonly ResultKind Kind;
        public readonly HaltStatus Halt; // meaningful when Kind == ResultKind.Halt
        public readonly StepDiff Diff;   // meaningful when Kind == ResultKind.Diff

        // Halt constructor
        public StepResult(HaltStatus haltStatus)
        {
            Kind = ResultKind.Halt;
            Halt = haltStatus;
            Diff = default;
        }

        // Diff constructor
        public StepResult(StepDiff diff)
        {
            Kind = ResultKind.Diff;
            Diff = diff;
            Halt = HaltStatus.None;
        }

        public bool IsHalt => Kind == ResultKind.Halt;
        public bool IsDiff => Kind == ResultKind.Diff;

        // Convenience accessors (throwing if wrong variant)
        public HaltStatus AsHalt()
        {
            if (!IsHalt) throw new InvalidOperationException("StepResult is not a Halt.");
            return Halt;
        }

        public StepDiff AsDiff()
        {
            if (!IsDiff) throw new InvalidOperationException("StepResult is not a Diff.");
            return Diff;
        }

        // Forwarded helpers for Diff variant
        public StepResult Inverse()
        {
            return !IsDiff ? new StepResult(HaltStatus.None) : new StepResult(Diff.Inverse());
        }

        public MoveDirection DirectionMoved
        {
            get
            {
                if (!IsDiff) throw new InvalidOperationException("No direction for halt result.");
                return Diff.DirectionMoved;
            }
        }

        public override string ToString()
        {
            return Kind == ResultKind.Diff
                ? $"Diff: step={Diff.StepIndex}: (oldState={Diff.PreviousState}, oldSymbol={Diff.SymbolBefore})" +
                  $" -> (newState={Diff.NextState}, newSymbol={Diff.SymbolAfter}, direction={DirectionMoved})"
                : $"Halt: {Halt}";
        }
    }
}