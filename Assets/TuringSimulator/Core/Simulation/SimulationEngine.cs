using System.Collections;
using System.Threading;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Simulation
{
    public class SimulationEngine : ISimulationEngine
    {
        public IEnumerator Run(
            IProgram program,
            ITape tape,
            ISimulationBuffer buffer,
            CancellationToken ct)
        {
            const int maxSteps = 100;
            var stepCount = 0;
            var currentState = program.StartState;

            while (!ct.IsCancellationRequested && stepCount < maxSteps)
            {
                // Accept only by being in / entering an Accept (final) state.
                if (program.IsFinalState(currentState))
                {
                    buffer.Complete(HaltStatus.Accept);
                    yield break;
                }

                var currentSymbol = tape.Read();
                var currentHeadIndex = tape.HeadIndex;

                // No matching transition: implicit halt as Reject.
                if (!program.TryGetTransition(currentState, currentSymbol, out var transition))
                {
                    buffer.Complete(HaltStatus.Reject);
                    yield break;
                }

                var symbolToWrite = transition.SymbolToWrite;
                var directionToMove = transition.DirectionToMove;
                var stateToGo = transition.ToState;

                tape.Write(symbolToWrite);
                tape.Move(directionToMove);

                var newHeadIndex = tape.HeadIndex;

                buffer.AddStepDiff(new StepDiff(
                    currentSymbol,
                    symbolToWrite,
                    currentHeadIndex,
                    newHeadIndex,
                    currentState,
                    stateToGo,
                    stepCount));

                currentState = stateToGo;
                stepCount++;

                yield return null;
            }

            if (ct.IsCancellationRequested)
                buffer.Complete(HaltStatus.Aborted);
            else if (stepCount >= maxSteps)
                buffer.Complete(HaltStatus.StepLimitExceeded);
        }
    }
}
