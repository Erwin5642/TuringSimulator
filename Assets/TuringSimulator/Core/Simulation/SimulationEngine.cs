using System;
using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Simulation
{
    public class SimulationEngine : ISimulationEngine
    {
        public async Task Run(
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
                var currentSymbol = tape.Read();
                var currentHeadIndex = tape.HeadIndex;

                if (!program.TryGetTransition(currentState, currentSymbol, out var transition))
                {
                    buffer.Complete(
                        program.IsFinalState(currentState)
                            ? HaltStatus.Accept
                            : HaltStatus.Reject);

                    return;
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
                
                await Task.Yield();
            }

            if (ct.IsCancellationRequested)
                buffer.Complete(HaltStatus.Aborted);
            else if (stepCount >= maxSteps)
                buffer.Complete(HaltStatus.StepLimitExceeded);
        }
    }
}
