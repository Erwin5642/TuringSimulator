using System.Threading.Tasks;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.View.Machine;
using UnityEngine;

namespace TuringSimulator.Controller.Syncronizer
{
    public class StepViewApplier : IStepApplier
    {
        public int CurrentStepIndex { get; private set; }
        private bool _isBusy;

        private readonly ISimulationBuffer _buffer;
        private readonly IMachineView _view;
        public StepViewApplier(ISimulationBuffer buffer, IMachineView view)
        {
            _buffer = buffer;
            _view = view;
            CurrentStepIndex = 0;
        }
        public async Task<StepResult?> TryStepForward()
        {
            if (_isBusy) return null;
            _isBusy = true;
            Debug.Log("[StepApplier] Trying to step");
            try
            {
                if (!_buffer.TryGetStep(CurrentStepIndex, out var step))
                {
                    Debug.Log("[StepApplier] No step avaiable");
                    return null;
                }
                
                Debug.Log("[StepApplier] Waiting for view");
                
                await _view.UpdateStepForward(step);

                Debug.Log($"[StepApplier] Step applied: {step}");
                
                CurrentStepIndex++;
                
                return step;
            }
            finally
            {
                _isBusy = false;  
                Debug.Log("[StepApplier] Step updated");
            }
        }

        public async Task<StepResult?> TryStepBackward()
        {
            if (_isBusy) return null;
            _isBusy = true;
            Debug.Log("[StepApplier] Trying to step");

            try
            {
                CurrentStepIndex--;

                if (!_buffer.TryGetStep(CurrentStepIndex, out var step))
                {
                    Debug.Log("[StepApplier] No step avaiable");
                    CurrentStepIndex++;
                    return null;
                }
                
                Debug.Log("[StepApplier] Waiting for view");
                await _view.UpdateStepBackward(step.Inverse());
                Debug.Log($"[StepApplier] Step applied: {step.Inverse()}");
                return step;
            }
            finally
            {
                _isBusy = false;  
                Debug.Log("[StepApplier] Step updated");
            }
        }

        public void Reset()
        {
            CurrentStepIndex = 0;
        }
    }
}