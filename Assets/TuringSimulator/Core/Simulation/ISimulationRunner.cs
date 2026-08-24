using System;
using System.Collections;
using System.Threading;
using TuringSimulator.Core.Simulation.Step;

namespace TuringSimulator.Core.Simulation
{
    /// <summary>
    /// Controls the simulation engine and stores the data needed for it.
    /// </summary>
    public interface ISimulationRunner
    {
        event Action<StepResult> OnStepProduced;
        event Action<SimulationRunResult> OnRunCompleted;

        /// <summary>
        /// Runs the simulation as a main-thread coroutine.
        /// Invokes <paramref name="onCompleted"/> when finished (including abort).
        /// </summary>
        IEnumerator Run(
            SimulationRunRequest request,
            Action<SimulationRunResult> onCompleted,
            CancellationToken cancellationToken = default);

        void Cancel();

        void Clear();
    }
}
