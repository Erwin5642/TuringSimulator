using System;
using System.Collections.Generic;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Simulation
{
    public readonly struct SimulationRunRequest
    {
        public SimulationRunRequest(IProgram program, SimulationTape tape)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
            Tape = tape ?? throw new ArgumentNullException(nameof(tape));
        }

        public IProgram Program { get; }
        public SimulationTape Tape { get; }
    }

    public readonly struct SimulationRunResult
    {
        public SimulationRunResult(HaltStatus haltStatus, IReadOnlyList<StepResult> steps, TapeSnapshot finalTape)
        {
            HaltStatus = haltStatus;
            Steps = steps ?? Array.Empty<StepResult>();
            FinalTape = finalTape;
        }

        public HaltStatus HaltStatus { get; }
        public IReadOnlyList<StepResult> Steps { get; }
        public TapeSnapshot FinalTape { get; }
        public int StepCount => Steps.Count;
    }
}
