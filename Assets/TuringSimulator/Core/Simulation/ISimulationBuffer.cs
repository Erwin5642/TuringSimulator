using System;
using System.Collections.Generic;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Simulation
{
    public interface ISimulationBuffer
    {
        event Action<StepResult> OnStepRecorded;
        event Action<HaltStatus> OnCompleted;

        /// <summary>
        /// Gets the halt status if the simulation has completed; or HaltStatus.None
        /// </summary>
        HaltStatus Status { get; }

        /// <summary>
        /// True while the simulation is still running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// True once the simulation has halted.
        /// </summary>
        bool IsHalted { get; }

        void AddStepDiff(StepDiff stepDiff);

        void Complete(HaltStatus status);

        bool TryGetStep(int index, out StepResult stepResult);

        IReadOnlyList<StepResult> Snapshot();

        void Clear();
    }
}