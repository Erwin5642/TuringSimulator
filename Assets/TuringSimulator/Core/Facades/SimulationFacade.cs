/*
using System;
using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Facades
{
    public class SimulationFacade : ISimulationFacade
    {
        public event Action<StepDiff> StepExecuted;
        public event Action<StepDiff> UndoStepExecuted;
        public event Action<HaltStatus> MachineHalted;
        public event Action SimulationCompleted;
    
        //private readonly SimulationController _controller;
        //private readonly DiffHistory _history;
    
        public SimulationFacade(SimulationController controller, DiffHistory history)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _history = history ?? throw new ArgumentNullException(nameof(history));
        }
    
        public async Task RunSimulationAsync(CancellationToken ct)
        {   
            await _controller.RunAsync(ct);
            SimulationCompleted?.Invoke();
        }
    
        public bool TryPlayNext()
        {
            if (!_history.TryForward(out var ev))
                return false;

            EmitEvent(ev);
            return true;
        }

        public bool TryPlayPrevious()
        {
            if (!_history.TryBackward(out var ev))
                return false;
        
            EmitEvent(ev);
            return true;
        }

        public void Reset()
        {
            _history.Clear();
            _controller.Reset();
        }

        private void EmitEvent(DiffEvent diffEvent)
        {
            switch (diffEvent)
            {
                case StepEvent stepEvent:
                    StepExecuted?.Invoke(stepEvent.Diff);
                    break;
                case UndoStepEvent undoStepEvent:
                    UndoStepExecuted?.Invoke(undoStepEvent.Diff);
                    break;
                case HaltEvent altEvent:
                    MachineHalted?.Invoke(altEvent.Status);
                    break;
            }
        }
    }
}
*/
