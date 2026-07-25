using System.Collections.Generic;
using System.Threading.Tasks;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.View.Machine;
using UnityEngine;

namespace TuringSimulator.Controller.Syncronizer
{
    public class StepViewApplier : IStepApplier
    {
        public int CurrentStepIndex { get; private set; }
        public int TotalSteps => _steps.Count;
        private bool _isBusy;

        private readonly List<StepResult> _steps = new();
        private readonly IMachineView _view;
        public StepViewApplier(IMachineView view)
        {
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
                if (CurrentStepIndex < 0 || CurrentStepIndex >= _steps.Count)
                {
                    Debug.Log("[StepApplier] No step avaiable");
                    return null;
                }

                var step = _steps[CurrentStepIndex];
                
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
                var targetStepIndex = CurrentStepIndex - 1;
                if (targetStepIndex < 0 || targetStepIndex >= _steps.Count)
                {
                    Debug.Log("[StepApplier] No step avaiable");
                    return null;
                }

                CurrentStepIndex = targetStepIndex;
                var step = _steps[targetStepIndex];
                
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
            _steps.Clear();
        }

        public void LoadSteps(IReadOnlyList<StepResult> steps)
        {
            _steps.Clear();
            if (steps != null)
            {
                for (var i = 0; i < steps.Count; i++)
                    _steps.Add(steps[i]);
            }

            CurrentStepIndex = 0;
        }
    }
}