using System.Threading.Tasks;
using System.Collections.Generic;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace Tests.EditModeTests.Mocks
{
    public sealed class BlockingSimulationBuffer : ISimulationBuffer
    {
        readonly List<StepResult> _history = new();
        private readonly TaskCompletionSource<bool> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private HaltStatus _status = HaltStatus.None;

        public Task Started => _started.Task;

        public void Release() => _release.TrySetResult(true);

        public event System.Action<StepResult> OnStepRecorded;
        public event System.Action<HaltStatus> OnCompleted;

        public HaltStatus Status => _status;
        public bool IsRunning => _status == HaltStatus.None;
        public bool IsHalted => _status != HaltStatus.None;

        public void AddStepDiff(StepDiff diff)
        {
            _started.TrySetResult(true);
            var stepResult = new StepResult(diff);
            _history.Add(stepResult);
            OnStepRecorded?.Invoke(stepResult);
            _release.Task.Wait(); // OK for test code
        }

        public void Complete(HaltStatus status)
        {
            _status = status;
            var stepResult = new StepResult(status);
            _history.Add(stepResult);
            OnStepRecorded?.Invoke(stepResult);
            OnCompleted?.Invoke(status);
        }

        public bool TryGetStep(int index, out StepResult stepResult)
        {
            stepResult = default;
            if (index < 0 || index >= _history.Count)
                return false;

            stepResult = _history[index];
            return true;
        }

        public IReadOnlyList<StepResult> Snapshot()
        {
            return _history.ToArray();
        }

        public void Clear()
        {
            _status = HaltStatus.None;
            _history.Clear();
        }
    }
}