using System.Collections.Generic;
using System.Threading;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Simulation
{
    public class SimulationBuffer : ISimulationBuffer
    {
        private readonly List<StepResult> _history = new();
        private readonly ReaderWriterLockSlim _lock = new();

        private HaltStatus _haltStatus;
        
        public bool IsRunning => Status == HaltStatus.None;
        public bool IsHalted => Status != HaltStatus.None;
        
        public void AddStepDiff(StepDiff stepDiff)
        {
            _lock.EnterWriteLock();
            try
            {
                _history.Add(new StepResult(stepDiff));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Complete(HaltStatus status)
        {
            _lock.EnterWriteLock();
            try
            {
                _haltStatus = status;
                _history.Add(new StepResult(status));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
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