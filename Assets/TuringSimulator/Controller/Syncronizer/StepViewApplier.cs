using System;
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
        private int _generation;

        private readonly List<StepResult> _steps = new();
        private readonly IMachineView _view;

        /// <summary>Raised with the step about to be animated forward (before the view await).</summary>
        public event Action<StepResult> OnStepApplying;

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
            var generation = _generation;
            try
            {
                if (CurrentStepIndex < 0 || CurrentStepIndex >= _steps.Count)
                {
                    Debug.Log("[StepApplier] No step avaiable");
                    return null;
                }

                var step = _steps[CurrentStepIndex];
                
                Debug.Log("[StepApplier] Waiting for view");
                OnStepApplying?.Invoke(step);
                await _view.UpdateStepForward(step);

                if (generation != _generation)
                    return null;

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
            var generation = _generation;

            try
            {
                var targetStepIndex = CurrentStepIndex - 1;
                if (targetStepIndex < 0 || targetStepIndex >= _steps.Count)
                {
                    Debug.Log("[StepApplier] No step avaiable");
                    return null;
                }

                var step = _steps[targetStepIndex];
                
                Debug.Log("[StepApplier] Waiting for view");
                await _view.UpdateStepBackward(step.Inverse());

                if (generation != _generation)
                    return null;

                CurrentStepIndex = targetStepIndex;
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
            _generation++;
            CurrentStepIndex = 0;
            _steps.Clear();
        }

        public void LoadSteps(IReadOnlyList<StepResult> steps)
        {
            _generation++;
            _steps.Clear();
            if (steps != null)
            {
                for (var i = 0; i < steps.Count; i++)
                    _steps.Add(steps[i]);
            }

            CurrentStepIndex = 0;
        }

        public void AppendStep(StepResult step)
        {
            _steps.Add(step);
        }

        public bool TryGetLastAppliedStep(out StepResult step)
        {
            var i = CurrentStepIndex - 1;
            if (i < 0 || i >= _steps.Count)
            {
                step = default;
                return false;
            }

            step = _steps[i];
            return true;
        }
    }
}
