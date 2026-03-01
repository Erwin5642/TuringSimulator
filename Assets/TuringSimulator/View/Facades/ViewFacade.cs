/*using System;
using TuringSimulator.Core.Machine;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace TuringSimulator.View.Facades
{
    public class ViewFacade : IViewFacade
    {
        public event Action ReadyForNextStep;

        private readonly MachineViewer _viewer;
        
        public ViewFacade(MachineViewer viewer)
        {
            _viewer = viewer ?? throw new ArgumentNullException(nameof(viewer));

            _viewer.StepCompleted += () => ReadyForNextStep?.Invoke();
        }

        public void ShowStep(StepDiff diff)
        {
            _viewer.PlayStep(diff);
            
        }

        public void ShowUndoStep(StepDiff diff)
        {
            _viewer.ReverseStep(diff);
        }

        public void ShowHalt(HaltStatus status)
        {
            _viewer.Halt(status);
        }

        public void Reset()
        {
            _viewer.Clear();
        }
    }
}*/