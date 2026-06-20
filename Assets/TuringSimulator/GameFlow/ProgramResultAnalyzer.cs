using System;
using System.Collections.Generic;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace TuringSimulator.GameFlow
{
    /// <summary>
    /// Builds coarse <see cref="ProgramResult"/> skill-evidence from the immutable program
    /// plus the executed step trace in <see cref="SimulationBuffer"/>.
    /// </summary>
    public static class ProgramResultAnalyzer
    {
        /// <summary>Analyze the last completed run (buffer ends with a halt step).</summary>
        public static ProgramResult Analyze(IProgram program, SimulationBuffer buffer)
        {
            if (program == null) throw new ArgumentNullException(nameof(program));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            var r = new ProgramResult();

            var halt = buffer.Status;
            r.ReachedAccept = halt == HaltStatus.Accept;
            r.ReachedReject = halt == HaltStatus.Reject;

            var traceStates = new List<int>();
            int i = 0;
            while (buffer.TryGetStep(i++, out var step))
            {
                if (step.IsHalt)
                    break;

                var d = step.AsDiff();
                var dir = d.DirectionMoved;
                if (dir == MoveDirection.Left || dir == MoveDirection.Right)
                    r.UsedMoveBlock = true;

                if (d.SymbolBefore != d.SymbolAfter)
                    r.UsedWriteBlock = true;

                if (d.SymbolBefore == Symbol.Mark || d.SymbolAfter == Symbol.Mark)
                    r.UsedMarkerSymbol = true;

                if (d.SymbolBefore == Symbol.Blank || d.SymbolAfter == Symbol.Blank)
                    r.UsedBlankAsTerminator = true;

                traceStates.Add(d.PreviousState);
                traceStates.Add(d.NextState);
            }

            AnalyzeProgramGraph(program, r, traceStates);

            return r;
        }

        static void AnalyzeProgramGraph(IProgram program, ProgramResult r, List<int> traceStates)
        {
            r.UsedConditionBlock = HasBranchingOnDistinctSymbols(program);
            r.BothConditionPortsWired = r.UsedConditionBlock;

            var traceLoop = TraceIndicatesStateRevisit(traceStates);
            r.HasLoop = traceLoop;
            // Heuristic: multi-body loops / richer control flow when many states and execution revisits a state.
            var stateCount = program.States != null ? program.States.Count : 0;
            r.DistinctLoopCount = stateCount >= 4 && traceLoop ? 2 : (traceLoop ? 1 : 0);

            var usedTypes = (r.UsedMoveBlock ? 1 : 0) + (r.UsedWriteBlock ? 1 : 0) +
                            (r.UsedConditionBlock ? 1 : 0);
            r.UsedAllBlockTypes = usedTypes >= 3;
        }

        static bool HasBranchingOnDistinctSymbols(IProgram program)
        {
            foreach (var state in program.States)
            {
                var transitions = new List<Transition>();
                foreach (Symbol sym in Enum.GetValues(typeof(Symbol)))
                {
                    if (program.TryGetTransition(state, sym, out var tr))
                        transitions.Add(tr);
                }

                if (transitions.Count < 2)
                    continue;

                var reference = transitions[0];
                for (var k = 1; k < transitions.Count; k++)
                {
                    if (!TransitionsEquivalent(reference, transitions[k]))
                        return true;
                }
            }

            return false;
        }

        static bool TransitionsEquivalent(Transition a, Transition b) =>
            a.ToState == b.ToState &&
            a.SymbolToWrite == b.SymbolToWrite &&
            a.DirectionToMove == b.DirectionToMove;

        static bool TraceIndicatesStateRevisit(IReadOnlyList<int> traceStates)
        {
            var seen = new HashSet<int>();
            foreach (var st in traceStates)
            {
                if (!seen.Add(st))
                    return true;
            }

            return false;
        }
    }
}
