using System;
using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Facades
{
    public interface ISimulationFacade
    {
        public event Action SimulationCompleted;
        public event Action<StepDiff> StepExecuted;
        public event Action<StepDiff> UndoStepExecuted;
        public event Action<HaltStatus> MachineHalted;
    
        public Task RunSimulationAsync(CancellationToken ct);
        public bool TryPlayNext();
        public bool TryPlayPrevious();
        public void Reset();
    }
}