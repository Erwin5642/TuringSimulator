using System.Threading.Tasks;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace Tests.EditModeTests.Mocks
{
    public sealed class BlockingSimulationBuffer : ISimulationBuffer
    {
        private readonly TaskCompletionSource<bool> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public void Release() => _release.TrySetResult(true);

        public HaltStatus Status { get; }
        public bool IsRunning { get; }
        public bool IsHalted { get; }

        public void AddStepDiff(StepDiff diff)
        {
            _started.TrySetResult(true);
            _release.Task.Wait(); // OK for test code
        }

        public void Complete(HaltStatus status) { }
        public bool TryGetStep(int index, out StepResult stepResult)
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }
    }
}