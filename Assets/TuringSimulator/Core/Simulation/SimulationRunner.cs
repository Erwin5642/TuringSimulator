using System;
using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Simulation.Step;

namespace TuringSimulator.Core.Simulation
{
    public class SimulationRunner : ISimulationRunner
    {
        private readonly SimulationEngine _engine;
        private readonly SimulationBuffer _buffer;
        private CancellationTokenSource _cts;

        public event Action<StepResult> OnStepProduced;
        public event Action<SimulationRunResult> OnRunCompleted;

        public SimulationRunner(SimulationBuffer buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _engine = new SimulationEngine();
        }

        public async Task<SimulationRunResult> Run(
            SimulationRunRequest request,
            CancellationToken cancellationToken = default)
        {
            var program = request.Program ?? throw new InvalidOperationException("Run request program cannot be null.");
            var tape = request.Tape ?? throw new InvalidOperationException("Run request tape cannot be null.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _buffer.Clear();
            _buffer.OnStepRecorded += HandleStepRecorded;

            SimulationRunResult result;
            try
            {
                await Task.Run(() => _engine.Run(program, tape, _buffer, _cts.Token), _cts.Token);
                result = new SimulationRunResult(
                    _buffer.Status,
                    _buffer.Snapshot(),
                    tape.Snapshot());
            }
            finally
            {
                _buffer.OnStepRecorded -= HandleStepRecorded;
                _cts.Dispose();
                _cts = null;
            }

            OnRunCompleted?.Invoke(result);
            return result;
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        public void Clear()
        {
            _buffer.Clear();
        }

        void HandleStepRecorded(StepResult step)
        {
            OnStepProduced?.Invoke(step);
        }
    }
}