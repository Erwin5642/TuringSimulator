using System.Collections.Generic;
using System.Threading.Tasks;
using TuringSimulator.Controller.Syncronizer;
using TuringSimulator.Core.Simulation.Step;

namespace Tests.PlayModeTests.Mocks
{
    public class FakeStepApplier : IStepApplier
    {
        private readonly List<StepResult> _steps;
        private int _currentIndex = -1;

        public FakeStepApplier(IEnumerable<StepResult> steps)
        {
            _steps = new List<StepResult>(steps);
        }

        public int CurrentStepIndex => _currentIndex;

        public Task<StepResult?> TryStepForward()
        {
            if (_currentIndex + 1 >= _steps.Count)
                return Task.FromResult<StepResult?>(null);

            _currentIndex++;
            return Task.FromResult<StepResult?>(_steps[_currentIndex]);
        }

        public Task<StepResult?> TryStepBackward()
        {
            if (_currentIndex < 0)
                return Task.FromResult<StepResult?>(null);

            var step = _steps[_currentIndex];
            _currentIndex--;
            return Task.FromResult<StepResult?>(step);
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}