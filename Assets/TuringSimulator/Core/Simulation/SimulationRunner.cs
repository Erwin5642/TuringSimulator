using System;
using System.Collections;
using System.Threading;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Tape;
using UnityEngine;

namespace TuringSimulator.Core.Simulation
{
    public class SimulationRunner : ISimulationRunner
    {
        private readonly SimulationEngine _engine;
        private readonly SimulationBuffer _buffer;
        private CancellationTokenSource _cts;
        private bool _running;

        public event Action<StepResult> OnStepProduced;
        public event Action<SimulationRunResult> OnRunCompleted;

        public SimulationRunner(SimulationBuffer buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _engine = new SimulationEngine();
        }

        public IEnumerator Run(
            SimulationRunRequest request,
            Action<SimulationRunResult> onCompleted,
            CancellationToken cancellationToken = default)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            var program = request.Program ?? throw new InvalidOperationException("Run request program cannot be null.");
            var tape = request.Tape ?? throw new InvalidOperationException("Run request tape cannot be null.");

            if (_running)
                throw new InvalidOperationException("A simulation is already running.");

            _running = true;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cts.Token;

            _buffer.Clear();
            _buffer.OnStepRecorded += HandleStepRecorded;

            var steps = _engine.Run(program, tape, _buffer, token);
            while (true)
            {
                object current;
                try
                {
                    if (!steps.MoveNext())
                        break;
                    current = steps.Current;
                }
                catch (Exception e)
                {
                    CleanupAfterRun();
                    Debug.LogError($"[SimulationRunner] Run failed: {e}");
                    yield break;
                }

                yield return current;
            }

            CleanupAfterRun();

            var result = new SimulationRunResult(
                _buffer.Status,
                _buffer.Snapshot(),
                tape.Snapshot());
            OnRunCompleted?.Invoke(result);
            onCompleted.Invoke(result);
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        public void Clear()
        {
            Cancel();
            CleanupAfterRun();
            _buffer.Clear();
        }

        void CleanupAfterRun()
        {
            _buffer.OnStepRecorded -= HandleStepRecorded;
            DisposeCts();
            _running = false;
        }

        void HandleStepRecorded(StepResult step)
        {
            OnStepProduced?.Invoke(step);
        }

        void DisposeCts()
        {
            if (_cts == null)
                return;

            _cts.Dispose();
            _cts = null;
        }
    }
}
