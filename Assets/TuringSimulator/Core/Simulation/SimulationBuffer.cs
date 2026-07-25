using System.Collections.Generic;
using System.Threading;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Simulation
{
    public class SimulationBuffer : ISimulationBuffer
    {
        private readonly List<StepResult> _history = new();
        private readonly ReaderWriterLockSlim _lock = new();

        private HaltStatus _haltStatus;

        public event System.Action<StepResult> OnStepRecorded;
        public event System.Action<HaltStatus> OnCompleted;
        
        public bool IsRunning => Status == HaltStatus.None;
        public bool IsHalted => Status != HaltStatus.None;
        
        public void AddStepDiff(StepDiff stepDiff)
        {
            var stepResult = new StepResult(stepDiff);
            _lock.EnterWriteLock();
            try
            {
                _history.Add(stepResult);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            OnStepRecorded?.Invoke(stepResult);
        }

        public void Complete(HaltStatus status)
        {
            var stepResult = new StepResult(status);
            _lock.EnterWriteLock();
            try
            {
                _haltStatus = status;
                _history.Add(stepResult);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            OnStepRecorded?.Invoke(stepResult);
            OnCompleted?.Invoke(status);
        }

        public bool TryGetStep(int index, out StepResult stepResult)
        {
            stepResult = default;

            _lock.EnterReadLock();
            try
            {
                if (index < 0 || index >= _history.Count)
                    return false;

                stepResult = _history[index];
                return true;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _haltStatus = HaltStatus.None;
                _history.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IReadOnlyList<StepResult> Snapshot()
        {
            _lock.EnterReadLock();
            try
            {
                return _history.ToArray();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        
        public HaltStatus Status
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _haltStatus;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }
    }
}