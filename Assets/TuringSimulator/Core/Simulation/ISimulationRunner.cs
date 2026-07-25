using System;
using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Simulation.Step;

namespace TuringSimulator.Core.Simulation
{
    /// <summary>
    ///  A simulation runner controls the simulation engine and store the data needed for it 
    /// </summary>
    public interface ISimulationRunner
    {
        event Action<StepResult> OnStepProduced;
        event Action<SimulationRunResult> OnRunCompleted;

        Task<SimulationRunResult> Run(SimulationRunRequest request, CancellationToken cancellationToken = default);
        void Cancel();

        void Clear();
    }
}